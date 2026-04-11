using Microsoft.EntityFrameworkCore;
using Notes.Application.Interfaces;
using Notes.Domain.Entities;

namespace Notes.Infrastructure.Persistence
{
    public class NoteRepository : INoteRepository
    {
        private readonly AppDbContext _context;

        public NoteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Notes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<List<Note>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Notes.ToListAsync(cancellationToken);
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken)
        {
            return await _context.Notes.CountAsync(cancellationToken);
        }

        public async Task AddAsync(Note note, CancellationToken cancellationToken)
        {
            await _context.Notes.AddAsync(note, cancellationToken);
        }

        public void Update(Note note)
        {
            _context.Notes.Update(note);
        }
    }
}
