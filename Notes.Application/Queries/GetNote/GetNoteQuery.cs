using MediatR;
using Notes.Domain.Entities;

namespace Notes.Application.Queries.GetNote
{
    public record GetNoteQuery(Guid Id) : IRequest<Note?>;
}
