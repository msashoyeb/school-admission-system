using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

public class SeatHoldConfiguration : IEntityTypeConfiguration<SeatHold>
{
    public void Configure(EntityTypeBuilder<SeatHold> builder)
    {
        builder.ToTable("holds", table =>
            table.HasCheckConstraint(
                "ck_holds_status_valid",
                "status IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ClassId)
            .HasColumnName("class_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.StudentId)
            .HasColumnName("student_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(x => x.ConfirmedAt)
            .HasColumnName("confirmed_at");

        builder.Property(x => x.CancelledAt)
            .HasColumnName("cancelled_at");

        builder.Property(x => x.ExpiredAt)
            .HasColumnName("expired_at");

        builder.HasOne(x => x.AdmissionClass)
            .WithMany(c => c.Holds)
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClassId, x.StudentId })
            .IsUnique()
            .HasDatabaseName("ux_holds_active_class_student")
            .HasFilter("status IN ('HELD', 'CONFIRMED')");

        builder.HasIndex(x => new { x.Status, x.ExpiresAt })
            .HasDatabaseName("ix_holds_status_expires_at");

        builder.HasIndex(x => new { x.ClassId, x.Status, x.ExpiresAt })
            .HasDatabaseName("ix_holds_class_status_expires_at");

        builder.HasIndex(x => new { x.StudentId, x.CreatedAt })
            .HasDatabaseName("ix_holds_student_created_at");
    }
}
