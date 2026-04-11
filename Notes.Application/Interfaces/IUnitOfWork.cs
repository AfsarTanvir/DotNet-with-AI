namespace Notes.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        INoteRepository Notes { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
