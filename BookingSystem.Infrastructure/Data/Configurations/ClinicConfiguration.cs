using BookingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Infrastructure.Data.Configurations;

public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Address).IsRequired().HasMaxLength(500);
        builder.Property(c => c.City).IsRequired().HasMaxLength(100);
        builder.Property(c => c.State).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ZipCode).IsRequired().HasMaxLength(20);
        builder.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Email).HasMaxLength(256);

        builder.Property(c => c.RowVersion).IsRowVersion().IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.ModifiedAt).IsRequired(false);

        builder.HasIndex(c => c.Name).HasDatabaseName("IX_Clinics_Name");
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasMany(c => c.Doctors)
            .WithOne(d => d.Clinic)
            .HasForeignKey(d => d.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Appointments)
            .WithOne(a => a.Clinic)
            .HasForeignKey(a => a.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
