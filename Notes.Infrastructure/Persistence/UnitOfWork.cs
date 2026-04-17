using Notes.Application.Interfaces;

namespace Notes.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public INoteRepository Notes { get; }
        public IUserRepository Users { get; }

        public UnitOfWork(AppDbContext context, INoteRepository noteRepository, IUserRepository userRepository)
        {
            _context = context;
            Notes = noteRepository;
            Users = userRepository;
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
