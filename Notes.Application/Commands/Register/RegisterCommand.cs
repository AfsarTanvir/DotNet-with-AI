using MediatR;

namespace Notes.Application.Commands.Register
{
    public record RegisterCommand(string FirstName, string LastName, string Email, string Password) : IRequest<string>;
}
