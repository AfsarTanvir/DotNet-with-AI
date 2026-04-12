using MediatR;

namespace Notes.Application.Commands.UpdateNote
{
    public record UpdateNoteCommand(Guid Id, string Title, string? Content) : IRequest<Unit>;
}
