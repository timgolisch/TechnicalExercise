using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;
using System.Diagnostics.Eventing.Reader;

namespace StargateAPI.Business.Commands
{
    public class CreatePerson : IRequest<CreatePersonResult>
    {
        public required string Name { get; set; } = string.Empty;
    }

    public class CreatePersonPreProcessor : IRequestPreProcessor<CreatePerson>
    {
        private readonly StargateContext _context;
        public CreatePersonPreProcessor(StargateContext context)
        {
            _context = context;
        }
        public Task Process(CreatePerson request, CancellationToken cancellationToken)
        {
            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            if (person is not null)
            {
                _context.Logs.AddAsync(new Log("Failure", $"Cannot add duplicate person: {request.Name}"));
                _context.SaveChangesAsync();
                throw new BadHttpRequestException("Bad Request: Duplicate name");
            }
            else if (request.Name.Trim() == "")
            {
                _context.Logs.AddAsync(new Log("Failure", $"Cannot add person with a blank name"));
                _context.SaveChangesAsync();
                throw new BadHttpRequestException("Bad Request: Invalid name");
            }

            return Task.CompletedTask;
        }
    }

    public class CreatePersonHandler : IRequestHandler<CreatePerson, CreatePersonResult>
    {
        private readonly StargateContext _context;

        public CreatePersonHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<CreatePersonResult> Handle(CreatePerson request, CancellationToken cancellationToken)
        {
            var newPerson = new Person()
            {
                Name = request.Name
            };

            await _context.People.AddAsync(newPerson);

            await _context.Logs.AddAsync(new Log("CreatePerson", $"Added - {request.Name}"));

            await _context.SaveChangesAsync();

            return new CreatePersonResult()
            {
                Id = newPerson.Id
            };
        }
    }

    public class CreatePersonResult : BaseResponse
    {
        public int Id { get; set; }
    }
}
