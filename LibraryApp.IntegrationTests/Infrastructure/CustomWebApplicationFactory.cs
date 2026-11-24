using LibraryApp.Application.Common;
using LibraryApp.Domain.Entities;
using LibraryApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace LibraryApp.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API in-memory against a throw-away PostgreSQL container.
/// Keycloak is replaced by <see cref="TestAuthHandler"/> (authentication) and fakes for
/// <see cref="IKeycloakAdminClient"/> / <see cref="IAuthTokenService"/>.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("libraryapp_tests")
        .WithUsername("libraryapp")
        .WithPassword("libraryapp")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Building the host happens lazily on first access to Services.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting writes into host configuration, which is visible to code in Program.cs that reads
        // configuration before builder.Build() (e.g. the Keycloak options used to set up JwtBearer).
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("Database:SeedSampleData", "false");
        builder.UseSetting("Keycloak:Authority", "http://keycloak.test/realms/libraryapp");
        builder.UseSetting("Keycloak:Realm", "libraryapp");
        builder.UseSetting("Keycloak:Audience", "account");
        builder.UseSetting("Keycloak:ClientId", "libraryapp-api");
        builder.UseSetting("Keycloak:ClientSecret", "test-secret");
        builder.UseSetting("Keycloak:AdminBaseUrl", "http://keycloak.test");
        builder.UseSetting("Keycloak:MetadataAddress", "http://keycloak.test/realms/libraryapp/.well-known/openid-configuration");
        builder.UseSetting("Keycloak:ValidIssuer", "http://keycloak.test/realms/libraryapp");

        builder.ConfigureTestServices(services =>
        {
            // Database: point the DbContext at the container regardless of how it was registered.
            services.RemoveAll<DbContextOptions<LibraryDbContext>>();
            services.AddDbContext<LibraryDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Authentication: header-driven test scheme becomes the default for everything,
            // so [Authorize] / [Authorize(Roles = ...)] never reach the JwtBearer handler.
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthHandler.SchemeName;
            });

            // External identity provider: no network calls to Keycloak.
            services.RemoveAll<IKeycloakAdminClient>();
            services.AddSingleton<IKeycloakAdminClient, FakeKeycloakAdminClient>();

            services.RemoveAll<IAuthTokenService>();
            services.AddSingleton<IAuthTokenService, FakeAuthTokenService>();
        });
    }

    /// <summary>Client without any identity (behaves like a request without a bearer token).</summary>
    public HttpClient CreateAnonymousClient() => CreateClient();

    /// <summary>Client authenticated as <paramref name="userId"/> with the given realm roles.</summary>
    public HttpClient CreateClientAs(Guid userId, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', roles));
        return client;
    }

    /// <summary>Seeds an author and a book directly through the DbContext and returns the book id.</summary>
    public async Task<Guid> SeedBookAsync(int totalStock = 3, string title = "Integration Test Book")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        var author = Author.Create("Integration Test Author");
        var book = Book.Create(author.Id, title, RandomIsbn13(), 99.90m, "TRY", totalStock);

        db.Authors.Add(author);
        db.Books.Add(book);
        await db.SaveChangesAsync();

        return book.Id;
    }

    private static string RandomIsbn13() =>
        "978" + Random.Shared.NextInt64(0, 10_000_000_000).ToString("D10");
}
