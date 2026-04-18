using Notes.Domain.Entities;

namespace Notes.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
        Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken);
        Task<List<User>> GetAllAsync(CancellationToken cancellationToken);
        Task<int> CountAsync(CancellationToken cancellationToken);

        Task AddAsync(User user, CancellationToken cancellationToken);
        void Update(User user);
    }
}
