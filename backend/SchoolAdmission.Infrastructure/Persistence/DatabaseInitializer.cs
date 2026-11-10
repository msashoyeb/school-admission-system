using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SchoolAdmission.Infrastructure.Persistence
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
        {
            if (EF.IsDesignTime)
            {
                return;
            }

            const int maxAttempts = 10;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var scope = services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
                    await dbContext.Database.MigrateAsync(cancellationToken);
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
                {
                    using var scope = services.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                        .CreateLogger(typeof(DatabaseInitializer));
                    logger.LogWarning(
                        ex,
                        "Database migration attempt {Attempt} of {MaxAttempts} failed. Retrying in 2 seconds...",
                        attempt,
                        maxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }
        }
    }
}