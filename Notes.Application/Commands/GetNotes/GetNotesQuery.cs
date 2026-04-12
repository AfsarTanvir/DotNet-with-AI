using MediatR;
using Notes.Domain.Entities;

namespace Notes.Application.Commands.GetNotes
{
    public record GetNotesQuery() : IRequest<List<Note>>;
}
