using BookingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Infrastructure.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired().HasMaxLength(450);
        builder.Property(p => p.Address).HasMaxLength(500);
        builder.Property(p => p.EmergencyContact).HasMaxLength(100);
        builder.Property(p => p.BloodGroup).HasMaxLength(10);
        builder.Property(p => p.MedicalHistory).HasMaxLength(4000);

        builder.Property(p => p.RowVersion).IsRowVersion().IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.ModifiedAt).IsRequired(false);

        builder.HasIndex(p => p.UserId).IsUnique().HasDatabaseName("IX_Patients_UserId");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasMany(p => p.Appointments)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
