using LibraryApp.Infrastructure.Persistence.Seed;

namespace LibraryApp.API.Extensions;

public static class DatabaseInitializationExtensions
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// "Database:ApplyMigrationsOnStartup" ve "Database:SeedSampleData" ayarlarına göre migration uygular / demo verisi ekler.
    /// Veritabanına ulaşılamazsa uygulama yine ayağa kalkar (hata loglanır, /health Unhealthy döner).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken ct = default)
    {
        var applyMigrations = app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");
        var seedSampleData = app.Configuration.GetValue<bool>("Database:SeedSampleData");

        if (!applyMigrations && !seedSampleData)
            return;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

                if (applyMigrations)
                    await initializer.MigrateAsync(ct);

                if (seedSampleData)
                    await initializer.SeedAsync(ct);

                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                app.Logger.LogWarning(ex, "Database initialization attempt {Attempt}/{MaxAttempts} failed, retrying in {Delay}s.",
                    attempt, MaxAttempts, RetryDelay.TotalSeconds);
                await Task.Delay(RetryDelay, ct);
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Database initialization failed after {MaxAttempts} attempts. The application will start without it.",
                    MaxAttempts);
            }
        }
    }
}
