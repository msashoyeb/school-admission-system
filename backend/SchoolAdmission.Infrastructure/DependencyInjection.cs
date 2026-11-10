using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Application;
using SchoolAdmission.Application.Interfaces;
using SchoolAdmission.Application.Services;
using SchoolAdmission.Infrastructure.BackgroundServices;
using SchoolAdmission.Infrastructure.Persistence;
using SchoolAdmission.Infrastructure.Repositories;
namespace SchoolAdmission.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = DatabaseConnectionResolver.Resolve(configuration);
            var holdDurationSeconds = int.TryParse(configuration["HOLD_DURATION_SECONDS"], out var parsed) && parsed > 0
                ? parsed
                : 600;

            services.AddSingleton(new AdmissionSeatHoldingTime
            {
                HoldDurationSeconds = holdDurationSeconds
            });

            services.AddDbContext<AdmissionDbContext>(options =>
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsAssembly(typeof(AdmissionDbContext).Assembly.FullName)));

            services.AddScoped<IAdmissionRepository, AdmissionRepository>();
            services.AddScoped<IAdmissionService, AdmissionService>();
            services.AddHostedService<HoldExpirationBackgroundService>();

            return services;
        }

    }
}
