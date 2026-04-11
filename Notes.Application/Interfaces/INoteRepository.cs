using Notes.Domain.Entities;

namespace Notes.Application.Interfaces
{
    public interface INoteRepository
    {
        Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<List<Note>> GetAllAsync(CancellationToken cancellationToken);
        Task<int> CountAsync(CancellationToken cancellationToken);

        Task AddAsync(Note note, CancellationToken cancellationToken);
        void Update(Note note);
    }
}
