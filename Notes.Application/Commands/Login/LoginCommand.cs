using MediatR;

namespace Notes.Application.Commands.Login
{
    public record LoginCommand(string Email, string Password) : IRequest<string>;
}
