using MediatR;
using Notes.Domain.Common;
using Notes.Domain.Entities;

namespace Notes.Domain.Events
{
    public class NoteCreatedEvent : INotification, IDomainEvent
    {
        public Note Note { get; }
        public NoteCreatedEvent(Note note)
        {
            Note = note;
        }
    }
}
