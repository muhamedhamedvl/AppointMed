using BookingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Infrastructure.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId).IsRequired().HasMaxLength(450);
        builder.Property(d => d.Specialization).IsRequired().HasMaxLength(100);
        builder.Property(d => d.LicenseNumber).IsRequired().HasMaxLength(50);
        builder.Property(d => d.ConsultationFee).HasColumnType("decimal(18,2)");
        builder.Property(d => d.AverageRating).HasColumnType("decimal(3,2)");
        builder.Property(d => d.Bio).HasMaxLength(2000);

        builder.Property(d => d.RowVersion).IsRowVersion().IsRequired();
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.ModifiedAt).IsRequired(false);

        builder.HasIndex(d => d.UserId).IsUnique().HasDatabaseName("IX_Doctors_UserId");
        builder.HasIndex(d => d.LicenseNumber).IsUnique().HasDatabaseName("IX_Doctors_LicenseNumber");
        builder.HasIndex(d => d.ClinicId).HasDatabaseName("IX_Doctors_ClinicId");
        builder.HasIndex(d => d.IsApproved).HasDatabaseName("IX_Doctors_IsApproved");
        builder.HasQueryFilter(d => !d.IsDeleted);

        builder.HasOne(d => d.Clinic)
            .WithMany(c => c.Doctors)
            .HasForeignKey(d => d.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.AvailableTimeSlots)
            .WithOne(t => t.Doctor)
            .HasForeignKey(t => t.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Appointments)
            .WithOne(a => a.Doctor)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
