using MediatR;
using Notes.Domain.Entities;

namespace Notes.Application.Commands.CreateUser
{
    public record CreateUserCommand(string FirstName, string LastName, string Email, string PasswordHash) : IRequest<User>;
}
