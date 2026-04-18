using MediatR;
using Microsoft.EntityFrameworkCore;
using Notes.Domain.Common;
using Notes.Domain.Entities;

namespace Notes.Infrastructure
{
    public class AppDbContext : DbContext
    {
        private readonly IMediator _mediator;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IMediator mediator) : base(options)
        {
            _mediator = mediator;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Note> Notes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(b => b.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<User>()
                .Property(b => b.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<User>()
                .HasQueryFilter(m => m.DeletedAt == null && m.IsActive);

            modelBuilder.Entity<Note>()
                .Property(b => b.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Note>()
                .Property(b => b.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Note>()
                .HasQueryFilter(n => n.DeletedAt == null);

            base.OnModelCreating(modelBuilder);
        }

        private async Task DispatchDomainEvents()
        {
            var entities = ChangeTracker
                .Entries<Entity>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            var domainEvents = entities
                .SelectMany(e => e.DomainEvents)
                .ToList();

            foreach (var entity in entities)
                entity.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
                await _mediator.Publish(domainEvent);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var result = await base.SaveChangesAsync(ct);

            await DispatchDomainEvents();

            return result;
        }
    }
}
