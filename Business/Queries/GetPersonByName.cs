using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetPersonByName : IRequest<GetPersonByNameResult>
    {
        public required string Name { get; set; } = string.Empty;
    }

    public class GetPersonByNameHandler : IRequestHandler<GetPersonByName, GetPersonByNameResult>
    {
        private readonly StargateContext _context;
        public GetPersonByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetPersonByNameResult> Handle(GetPersonByName request, CancellationToken cancellationToken)
        {
            var result = new GetPersonByNameResult();

            var parameters = new DynamicParameters();
            parameters.Add("@Name", request.Name);

            var query = "SELECT p.Id as PersonId, p.Name, d.CurrentRank, d.CurrentDutyTitle, d.CareerStartDate, d.CareerEndDate " + 
                "FROM [Person] p LEFT JOIN [AstronautDetail] d on p.Id = d.PersonId " + 
                $"WHERE p.Name = @Name";

            var person = await _context.Connection.QueryAsync<PersonAstronaut>(query, parameters);

            result.Person = person.FirstOrDefault();

            return result;
        }
    }

    public class GetPersonByNameResult : BaseResponse
    {
        public PersonAstronaut? Person { get; set; }
    }
}
