using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;
using System.Diagnostics.Eventing.Reader;

//---- Changes / Notes -----------
// 1. I don't actually use this, but it might be useful later, if we choose to log exceptions from the Controllers or the UI
//--------------------------------

namespace StargateAPI.Business.Commands
{
    public class CreateLog : IRequest<CreateLogResult>
    {
        public required string Type { get; set; } = string.Empty;
        public required string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
    }

    public class CreateLogHandler : IRequestHandler<CreateLog, CreateLogResult>
    {
        private readonly StargateContext _context;

        public CreateLogHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<CreateLogResult> Handle(CreateLog request, CancellationToken cancellationToken)
        {

            var newLog = new Data.Log()
            {
                Type = request.Type,
                Title = request.Title,
                Description = request.Description,
                StackTrace = request.StackTrace
            };

            //await _context.Log.AddAsync(newLog);

            await _context.SaveChangesAsync();

            return new CreateLogResult()
            {
                Id = newLog.Id
            };
        }
    }

    public class CreateLogResult : BaseResponse
    {
        public int Id { get; set; }
    }
}
