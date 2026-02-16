using Dapper;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class UpdateAstronautDuty : IRequest<UpdateAstronautDutyResult>
    {
        public required int Id { get; set; }

        public string Name { get; set; }

        public string Rank { get; set; }

        public string DutyTitle { get; set; }

        public DateTime? DutyStartDate { get; set; }

        public DateTime? DutyEndDate { get; set; }
    }

    public class UpdateAstronautDutyPreProcessor : IRequestPreProcessor<UpdateAstronautDuty>
    {
        private readonly StargateContext _context;

        public UpdateAstronautDutyPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public Task Process(UpdateAstronautDuty request, CancellationToken cancellationToken)
        {
            var astronautDuty = _context.AstronautDuties.AsNoTracking().FirstOrDefault(p => p.Id == request.Id);

            if (astronautDuty is null)
            {
                _context.Logs.AddAsync(new Log("Failure", $"Cannot find duty with ID: {request.Id}"));
                _context.SaveChangesAsync();
                throw new BadHttpRequestException("Bad Request: Duty not found");
            }

            var person = _context.People.AsNoTracking().FirstOrDefault(p => p.Name == request.Name);

            if (person is null)
            {
                _context.Logs.AddAsync(new Log("Failure", $"Cannot change duty to an unknown person: {request.Name}"));
                _context.SaveChangesAsync();
                throw new BadHttpRequestException("Bad Request: person not found");
            }
            else if (person.Id != astronautDuty.PersonId)
            {
                //assigning this duty record to another person is more-involved
                //Check for a duplicate
                var verifyNoPreviousDuty = _context.AstronautDuties.AsNoTracking()
                    .FirstOrDefault(z => z.PersonId == person.Id && z.DutyTitle == request.DutyTitle && z.DutyStartDate == request.DutyStartDate);

                if (verifyNoPreviousDuty is null)
                {
                    _context.Logs.AddAsync(new Log("Failure", $"Cannot add duplicate duty for: {person.Name} - {request.DutyTitle}, {request.DutyStartDate}"));
                    _context.SaveChangesAsync();
                    throw new BadHttpRequestException("Bad Request: Duplicate duty entry");
                }
            }
            //Note: Ask if we should also prevent an entry that pre-dates an existing entry, which could cause overlaps

            return Task.CompletedTask;
        }
    }

    public class UpdateAstronautDutyHandler : IRequestHandler<UpdateAstronautDuty, UpdateAstronautDutyResult>
    {
        private readonly StargateContext _context;

        public UpdateAstronautDutyHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<UpdateAstronautDutyResult> Handle(UpdateAstronautDuty request, CancellationToken cancellationToken)
        {
            var person = _context.People.AsNoTracking().FirstOrDefault(p => p.Name == request.Name);
            int originalPersonId = 0;

            //Find the existing Duty, if there is one
            var astronautDuty = _context.AstronautDuties.FirstOrDefault(ad => ad.Id == request.Id);

            if (astronautDuty != null)
            {
                originalPersonId = astronautDuty.PersonId;
                if (person != null && person.Id != originalPersonId) astronautDuty.PersonId = person.Id;
                if (request.Rank.Trim() != "") astronautDuty.Rank = request.Rank;
                if (request.DutyTitle.Trim() != "") astronautDuty.DutyTitle = request.DutyTitle;
                if (request.DutyStartDate != null) astronautDuty.DutyStartDate = request.DutyStartDate.Value;
                astronautDuty.DutyEndDate = request.DutyEndDate;

                _context.AstronautDuties.Update(astronautDuty);

                //The current/active AstronautDuty record will have a null DutyEndDate
                if (astronautDuty.DutyEndDate == null || request.DutyTitle == "RETIRED")
                {
                    var astronautDetail = _context.AstronautDetails.FirstOrDefault(ad => ad.PersonId == originalPersonId);

                    //AstronautDuty holds the current Detail. So changes are also applied to AstronautDetail
                    if (astronautDetail != null)
                    {
                        if (person != null && person.Id != originalPersonId) astronautDetail.PersonId = person.Id;
                        astronautDetail.CurrentRank = astronautDuty.Rank;
                        astronautDetail.CurrentDutyTitle = astronautDuty.DutyTitle;
                        if (request.DutyTitle == "RETIRED")
                        {
                            astronautDetail.CareerEndDate = astronautDuty.DutyStartDate.AddDays(-1).Date;
                        }
                        else
                        {
                            astronautDetail.CareerEndDate = null;
                        }
                        _context.AstronautDetails.Update(astronautDetail);
                    }
                }
            }

            await _context.Logs.AddAsync(new Log("UpdateAstronautDuty", $"Updated - {request.Rank}, {request.DutyTitle} for {person.Name}"));

            await _context.SaveChangesAsync();

            return new UpdateAstronautDutyResult()
            {
                Id = astronautDuty.Id
            };
        }
    }

    public class UpdateAstronautDutyResult : BaseResponse
    {
        public int? Id { get; set; }
    }
}
