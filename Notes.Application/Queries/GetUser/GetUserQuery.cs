using MediatR;
using Notes.Domain.Entities;

namespace Notes.Application.Queries.GetUser
{
    public record GetUserQuery(Guid Id) : IRequest<User?>;
}
