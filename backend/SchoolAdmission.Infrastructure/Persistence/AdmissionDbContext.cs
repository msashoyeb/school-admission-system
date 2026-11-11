using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Infrastructure.Persistence
{
    public class AdmissionDbContext(DbContextOptions<AdmissionDbContext> options) : DbContext(options)
    {
        public DbSet<AdmissionClass> Classes => Set<AdmissionClass>();
        public DbSet<SeatHold> Holds => Set<SeatHold>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdmissionDbContext).Assembly);
        }
    }
}

// dotnet ef migrations add InitialCreate --project backend/SchoolAdmission.Infrastructure --startup-project backend/SchoolAdmission.Api --output-dir Persistence/Migrations --no-build
// dotnet ef database update --project backend/SchoolAdmission.Infrastructure --startup-project backend/SchoolAdmission.Api