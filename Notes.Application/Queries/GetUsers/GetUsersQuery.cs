using MediatR;
using Notes.Domain.Entities;

namespace Notes.Application.Queries.GetUsers
{
    public record GetUsersQuery : IRequest<List<User>>;
}
