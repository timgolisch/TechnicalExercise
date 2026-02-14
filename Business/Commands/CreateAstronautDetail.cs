using Dapper;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class CreateAstronautDetail : IRequest<CreateAstronautDetailResult>
    {
        public required string Name { get; set; }

        public required string CurrentRank { get; set; }

        public required string CurrentTitle { get; set; }

        public DateTime CareerStartDate { get; set; }

        public DateTime CareerEndDate { get; set; }
    }

    public class CreateAstronautDetailPreProcessor : IRequestPreProcessor<CreateAstronautDetail>
    {
        private readonly StargateContext _context;

        public CreateAstronautDetailPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public Task Process(CreateAstronautDetail request, CancellationToken cancellationToken)
        {
            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            if (person is null) throw new BadHttpRequestException("Bad Request");

            var verifyNoPreviousDuty = _context.AstronautDuties.FirstOrDefault(z => z.CurrentTitle == request.CurrentTitle && z.CareerStartDate == request.CareerStartDate);

            if (verifyNoPreviousDuty is not null) throw new BadHttpRequestException("Bad Request");

            return Task.CompletedTask;
        }
    }

    public class CreateAstronautDetailHandler : IRequestHandler<CreateAstronautDetail, CreateAstronautDetailResult>
    {
        private readonly StargateContext _context;

        public CreateAstronautDetailHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<CreateAstronautDetailResult> Handle(CreateAstronautDetail request, CancellationToken cancellationToken)
        {
            await new CreateAstronautDetailPreProcessor(_context).Process(request, cancellationToken);

            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            var astronautDetail = _context.AstronautDetails.AsNoTracking().FirstOrDefault(z => z.PersonId == person.Id);

            if (astronautDetail == null)
            {
                astronautDetail = new AstronautDetail();
                astronautDetail.PersonId = person.Id;
                astronautDetail.CurrentDutyTitle = request.CurrentTitle;
                astronautDetail.CurrentRank = request.CurrentRank;
                astronautDetail.CareerStartDate = request.CareerStartDate.Date;
                if (request.CurrentTitle == "RETIRED")
                {
                    astronautDetail.CareerEndDate = request.CareerStartDate.Date;
                }

                await _context.AstronautDetails.AddAsync(astronautDetail);

            }
            else
            {
                astronautDetail.CurrentCurrentTitle = request.CurrentTitle;
                astronautDetail.CurrentCurrenRank = request.CurrenRank;
                if (request.CurrentTitle == "RETIRED")
                {
                    astronautDetail.CareerEndDate = request.CareerStartDate.AddDays(-1).Date;
                }
                _context.AstronautDetails.Update(astronautDetail);
            }

            //Note: this looks like raw SQL string: Injection attack vulnerability
            query = $"SELECT * FROM [AstronautDetail] WHERE {person.Id} = PersonId Order By CareerStartDate Desc";

            var AstronautDetail = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDetail>(query);

            if (AstronautDetail != null)
            {
                AstronautDetail.DutyEndDate = request.CareerStartDate.AddDays(-1).Date;
                _context.AstronautDuties.Update(AstronautDetail);
            }

            var newAstronautDetail = new AstronautDetail()
            {
                PersonId = person.Id,
                CurrenRank = request.CurrenRank,
                CurrentTitle = request.CurrentTitle,
                CareerStartDate = request.CareerStartDate.Date,
                DutyEndDate = null
            };

            await _context.AstronautDuties.AddAsync(newAstronautDetail);

            await _context.SaveChangesAsync();

            return new CreateAstronautDetailResult()
            {
                Id = newAstronautDetail.Id
            };
        }
    }

    public class CreateAstronautDetailResult : BaseResponse
    {
        public int? Id { get; set; }
    }
}
