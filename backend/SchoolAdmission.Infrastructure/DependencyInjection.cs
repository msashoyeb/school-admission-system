using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Infrastructure.Persistence;
namespace SchoolAdmission.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = DatabaseConnectionResolver.Resolve(configuration);

            services.AddDbContext<AdmissionDbContext>(options =>
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsAssembly(typeof(AdmissionDbContext).Assembly.FullName)));

            return services;
        }
    }
}
