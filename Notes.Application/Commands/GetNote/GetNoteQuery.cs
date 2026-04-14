using MediatR;
using Notes.Domain.Entities;

namespace Notes.Application.Commands.GetNote
{
    public record GetNoteQuery(Guid Id) : IRequest<Note?>;
}
