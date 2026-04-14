using MediatR;

namespace Notes.Application.Commands.CreateNote
{
    public record CreateNoteCommand(string Title, string? Content, Guid UserId) : IRequest<Domain.Entities.Note>;
}
