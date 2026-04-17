namespace Notes.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        INoteRepository Notes { get; }
        IUserRepository Users { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
