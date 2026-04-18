using MediatR;
using Notes.Domain.Events;
using Serilog;

public class NoteCreatedEventHandler
    : INotificationHandler<NoteCreatedEvent>
{
    public Task Handle(NoteCreatedEvent notification, CancellationToken cancellationToken)
    {
        Log.Information("Note created with Id {NoteId}", notification.Note.Id);
        return Task.CompletedTask;
    }
}