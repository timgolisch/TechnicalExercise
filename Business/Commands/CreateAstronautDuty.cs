using Dapper;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        public required string Name { get; set; }

        public required string Rank { get; set; }

        public required string DutyTitle { get; set; }

        public required DateTime DutyStartDate { get; set; }

        public DateTime DutyEndDate { get; set; }
    }

    public class CreateAstronautDutyPreProcessor : IRequestPreProcessor<CreateAstronautDuty>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public Task Process(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            var person = _context.People.AsNoTracking().FirstOrDefault(p => p.Name == request.Name);

            if (person is null)
            {
                _context.Logs.AddAsync(new Log("Failure", $"Cannot add duty for unknown person: {request.Name}"));
                throw new BadHttpRequestException("Bad Request: person not found");
            }

            //Note: This would be more effective if it also prevented an entry that pre-dates an existing entry, which would cause overlaps
            //Check for a duplicate
            var verifyNoPreviousDuty = _context.AstronautDuties.AsNoTracking().FirstOrDefault(z => z.DutyTitle == request.DutyTitle && z.DutyStartDate == request.DutyStartDate);

            if (verifyNoPreviousDuty is not null)
            {
                _context.Logs.AddAsync(new Log("Failure", $"Cannot add duplicate duty for: {person.Name} - {request.DutyTitle}, {request.DutyStartDate}"));
                throw new BadHttpRequestException("Bad Request: Duplicate duty entry");
            }

            return Task.CompletedTask;
        }
    }

    public class CreateAstronautDutyHandler : IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<CreateAstronautDutyResult> Handle(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            await (new CreateAstronautDutyPreProcessor(_context)).Process(request, cancellationToken);

            var person = _context.People.AsNoTracking().FirstOrDefault(p => p.Name == request.Name);

            //AstronautDuty holds the current Detail. So changes to the current duty are copied into AstronautDetail
            var astronautDetail = _context.AstronautDetails.FirstOrDefault(ad => ad.PersonId == person.Id);

            if (astronautDetail == null)
            {
                astronautDetail = new AstronautDetail();
                astronautDetail.PersonId = person.Id;
                astronautDetail.CurrentDutyTitle = request.DutyTitle;
                astronautDetail.CurrentRank = request.Rank;
                astronautDetail.CareerStartDate = request.DutyStartDate.Date;
                
                if (request.DutyTitle == "RETIRED")
                {
                    astronautDetail.CareerEndDate = request.DutyStartDate.Date;
                }

                await _context.AstronautDetails.AddAsync(astronautDetail);

            }
            else
            {
                astronautDetail.CurrentDutyTitle = request.DutyTitle;
                astronautDetail.CurrentRank = request.Rank;
                if (request.DutyTitle == "RETIRED")
                {
                    astronautDetail.CareerEndDate = request.DutyStartDate.AddDays(-1).Date;
                }
                _context.AstronautDetails.Update(astronautDetail);
            }

            //de-activate the current Duty, if there is one
            var previousDuty = _context.AstronautDuties
                .Where(ad => ad.PersonId == person.Id)
                .OrderBy(ad => ad.DutyStartDate)
                .LastOrDefault();

            if (previousDuty != null)
            {
                previousDuty.DutyEndDate = request.DutyStartDate.AddDays(-1).Date;
                _context.AstronautDuties.Update(previousDuty);
            }

            //Then add the newest record
            var newAstronautDuty = new AstronautDuty()
            {
                PersonId = person.Id,
                Rank = request.Rank,
                DutyTitle = request.DutyTitle,
                DutyStartDate = request.DutyStartDate.Date,
                DutyEndDate = null
            };

            await _context.AstronautDuties.AddAsync(newAstronautDuty);

            await _context.SaveChangesAsync();

            await _context.Logs.AddAsync(new Log("CreateAstronautDuty", $"Added - {request.Rank}, {request.DutyTitle} for {person.Name}"));

            return new CreateAstronautDutyResult()
            {
                Id = newAstronautDuty.Id
            };
        }
    }

    public class CreateAstronautDutyResult : BaseResponse
    {
        public int? Id { get; set; }
    }
}
