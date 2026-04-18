using Notes.Domain.Common;
using Notes.Domain.Events;

namespace Notes.Domain.Entities
{
    public class Note : Entity
    {
        public string Title { get; private set; }
        public string? Content { get; private set; }
        public Guid CreatedBy { get; private set; }

        public Note(string title, string? content, Guid createdBy)
        {
            SetTitle(title);
            Content = content;
            CreatedBy = createdBy;

            AddDomainEvent(new NoteCreatedEvent(this));
        }

        public void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty");

            Title = title;
        }

        public void UpdateContent(string content)
        {
            Content = content;
        }
    }
}
