using MediatR;
using Notes.Domain.Entities;

namespace Notes.Application.Queries.GetNotes
{
    public record GetNotesQuery() : IRequest<List<Note>>;
}
