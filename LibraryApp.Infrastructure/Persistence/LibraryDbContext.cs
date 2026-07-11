using LibraryApp.Application.Common;
using LibraryApp.Domain.Common;
using LibraryApp.Domain.Entities;
using LibraryApp.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryApp.Infrastructure.Persistence
{
    public class LibraryDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Author> Authors { get; set; }

        private readonly IMediator _mediator;
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options,IMediator mediator) : base(options)
        {
            _mediator = mediator;
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            // 1. Save öncesi, event'i olan tüm AggregateRoot'ları topla
            var aggregatesWithEvents = ChangeTracker.Entries<AggregateRoot>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Any())
                .ToList();

            // 2. Önce veritabanına yaz
            var result = await base.SaveChangesAsync(ct);

            // 3. Save başarılıysa event'leri yayınla
            foreach (var aggregate in aggregatesWithEvents)
            {
                var events = aggregate.DomainEvents.ToList();
                aggregate.ClearDomainEvents();

                foreach (var domainEvent in events)
                { 
                    var closedType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                    var notification = Activator.CreateInstance(closedType, domainEvent);
                    await _mediator.Publish(notification!, ct);
                }
            }

            return result;
        }
    }
}
