using LibraryApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApp.Infrastructure.Persistence.Seed;

// Uygulama açılışında (konfigürasyona bağlı) migration uygular ve demo verisi ekler.
public class DatabaseInitializer(LibraryDbContext dbContext, ILogger<DatabaseInitializer> logger)
{
    // Keycloak realm import'unda demo müşteri bu sabit id ile oluşturulur (Member.Id == Keycloak "sub").
    public const string DemoCustomerExternalId = "c0000000-0000-0000-0000-000000000001";
    public const string DemoCustomerEmail = "customer@libraryapp.local";

    public async Task MigrateAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync(ct);
        logger.LogInformation("Database migrations applied.");
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await dbContext.Authors.IgnoreQueryFilters().AnyAsync(ct))
        {
            logger.LogInformation("Sample data already present, skipping seed.");
            return;
        }

        var robertMartin = Author.Create("Robert C. Martin");
        var martinFowler = Author.Create("Martin Fowler");
        var ericEvans = Author.Create("Eric Evans");

        dbContext.Authors.AddRange(robertMartin, martinFowler, ericEvans);

        dbContext.Books.AddRange(
            Book.Create(robertMartin.Id, "Clean Code", "9780132350884", 450m, "TRY", 5),
            Book.Create(robertMartin.Id, "Clean Architecture", "9780134494166", 520m, "TRY", 3),
            Book.Create(martinFowler.Id, "Refactoring", "9780134757599", 610m, "TRY", 4),
            Book.Create(martinFowler.Id, "Patterns of Enterprise Application Architecture", "9780321127426", 700m, "TRY", 2),
            Book.Create(ericEvans.Id, "Domain-Driven Design", "9780321125217", 680m, "TRY", 3));

        var demoCustomerId = Guid.Parse(DemoCustomerExternalId);
        if (!await dbContext.Members.IgnoreQueryFilters().AnyAsync(m => m.Id == demoCustomerId, ct))
        {
            dbContext.Members.Add(Member.Create("Demo", "Customer", DemoCustomerEmail, externalIdentityId: DemoCustomerExternalId));
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Sample data seeded.");
    }
}
