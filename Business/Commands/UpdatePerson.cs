using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

//---- Changes / Notes -----------
// 1. New file. Lots of thinking about business rules.
//    Since this only changes the Name, I think I covered all of the angles,
//    but if I had a co-worker (BA or tester) I would certainly ask for a review
//--------------------------------

namespace StargateAPI.Business.Commands
{
    public class UpdatePerson : IRequest<UpdatePersonResult>
    {
        public required string Name { get; set; } = string.Empty;
        public required string NewName { get; set; } = string.Empty;
    }

    public class UpdatePersonPreProcessor : IRequestPreProcessor<UpdatePerson>
    {
        private readonly StargateContext _context;
        public UpdatePersonPreProcessor(StargateContext context)
        {
            _context = context;
        }
        public Task Process(UpdatePerson request, CancellationToken cancellationToken)
        {
            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            if (person is null)
            {
                _context.Logs.AddAsync(new Log("Failure", $"Cannot Update. Person: {request.Name} is not found."));
                _context.SaveChangesAsync();
                throw new BadHttpRequestException("Bad Request: Name not found");
            }
            else if (request.Name.Trim() == request.NewName.Trim())
            {
                _context.Logs.AddAsync(new Log("Failure", $"Cannot change Person Name from: {request.Name} to {request.NewName} because they are the same."));
                _context.SaveChangesAsync();
                throw new BadHttpRequestException("Bad Request: Nothing to change");
            }
            else if (request.NewName.Trim() == "")
            {
                _context.Logs.AddAsync(new Log("Failure", $"Cannot change Person Name from: {request.Name} to [blank]."));
                _context.SaveChangesAsync();
                throw new BadHttpRequestException("Bad Request: Invalid change");
            }

            return Task.CompletedTask;
        }
    }

    public class UpdatePersonHandler : IRequestHandler<UpdatePerson, UpdatePersonResult>
    {
        private readonly StargateContext _context;

        public UpdatePersonHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<UpdatePersonResult> Handle(UpdatePerson request, CancellationToken cancellationToken)
        {
            var person = _context.People.FirstOrDefault(z => z.Name == request.Name);

            person.Name = request.NewName;

            _context.People.Update(person);

            await _context.Logs.AddAsync(new Log("UpdatePerson", $"Updated {request.Name} to {request.NewName}"));

            await _context.SaveChangesAsync();

            return new UpdatePersonResult()
            {
                Id = person.Id,
                Name = request.NewName
            };
        }
    }

    public class UpdatePersonResult : BaseResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
