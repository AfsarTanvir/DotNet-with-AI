using MediatR;

namespace Notes.Application.Commands.DeleteNotes
{
    public record DeleteNoteCommand(Guid Id) : IRequest<Unit>;
}
