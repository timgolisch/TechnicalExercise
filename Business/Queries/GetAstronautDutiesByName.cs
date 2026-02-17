using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

//---- Changes / Notes -----------
// 1. The query had a SQL Injection vulnerability. I changed it to use a parmeterized query
// 2. The Person query seemed unnecessary. I added Person/Name to the 2nd query for AstronautDuty
// 3. I also formatted the query to make it a little more readable
//--------------------------------

namespace StargateAPI.Business.Queries
{
    public class GetAstronautDutiesByName : IRequest<GetAstronautDutiesByNameResult>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class GetAstronautDutiesByNameHandler : IRequestHandler<GetAstronautDutiesByName, GetAstronautDutiesByNameResult>
    {
        private readonly StargateContext _context;

        public GetAstronautDutiesByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetAstronautDutiesByNameResult> Handle(GetAstronautDutiesByName request, CancellationToken cancellationToken)
        {

            var result = new GetAstronautDutiesByNameResult();

            var parameters = new DynamicParameters();
            parameters.Add("@Name", request.Name);
            
            string query = "SELECT d.* " + 
                "FROM [Person] p LEFT JOIN [AstronautDuty] d on p.Id = d.PersonId " + 
                "WHERE p.Name = @Name " + 
                "ORDER BY d.DutyStartDate Desc";

            var duties = await _context.Connection.QueryAsync<AstronautDuty>(query, parameters);

            result.AstronautDuties = duties.ToList();

            return result;

        }
    }

    public class GetAstronautDutiesByNameResult : BaseResponse
    {
        public List<AstronautDuty> AstronautDuties { get; set; } = new List<AstronautDuty>();
    }
}
