using Notes.Application.Interfaces;

namespace Notes.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public INoteRepository Notes { get; }

        public UnitOfWork(AppDbContext context, INoteRepository noteRepository)
        {
            _context = context;
            Notes = noteRepository;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
