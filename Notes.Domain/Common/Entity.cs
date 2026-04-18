namespace Notes.Domain.Common
{
    public abstract class Entity
    {
        public Guid Id { get; protected set; }

        private List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(IDomainEvent eventItem)
        {
            _domainEvents.Add(eventItem);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        public DateTime CreatedAt { get; protected set; }
        public DateTime? UpdatedAt { get; protected set; }

        public DateTime? DeletedAt { get; protected set; }

        public bool IsDeleted => DeletedAt.HasValue;

        public void SoftDelete()
        {
            if (IsDeleted)
                throw new Exception("Already deleted");

            DeletedAt = DateTime.UtcNow;
        }

        protected void EnsureNotDeleted()
        {
            if (IsDeleted)
                throw new Exception("Entity is deleted");
        }
    }
}
