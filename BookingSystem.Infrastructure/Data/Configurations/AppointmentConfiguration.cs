using BookingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AppointmentDate).IsRequired();
        builder.Property(a => a.Status).HasConversion<int>().IsRequired();
        builder.Property(a => a.ReasonForVisit).HasMaxLength(500);
        builder.Property(a => a.Notes).HasMaxLength(2000);
        builder.Property(a => a.CancellationReason).HasMaxLength(500);
        builder.Property(a => a.CancelledAt).IsRequired(false);

        builder.Property(a => a.RowVersion).IsRowVersion().IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.ModifiedAt).IsRequired(false);

        builder.HasIndex(a => a.PatientId).HasDatabaseName("IX_Appointments_PatientId");
        builder.HasIndex(a => a.DoctorId).HasDatabaseName("IX_Appointments_DoctorId");
        builder.HasIndex(a => a.ClinicId).HasDatabaseName("IX_Appointments_ClinicId");
        builder.HasIndex(a => a.AppointmentDate).HasDatabaseName("IX_Appointments_AppointmentDate");
        builder.HasIndex(a => a.Status).HasDatabaseName("IX_Appointments_Status");
        builder.HasIndex(a => a.SlotId).IsUnique().HasDatabaseName("IX_Appointments_SlotId");
        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Clinic)
            .WithMany(c => c.Appointments)
            .HasForeignKey(a => a.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Slot)
            .WithOne(t => t.Appointment)
            .HasForeignKey<Appointment>(a => a.SlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
