using BookingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Infrastructure.Data.Configurations;

public class AvailableTimeSlotConfiguration : IEntityTypeConfiguration<AvailableTimeSlot>
{
    public void Configure(EntityTypeBuilder<AvailableTimeSlot> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.IsBooked).HasDefaultValue(false);
        builder.Property(t => t.RowVersion).IsRowVersion().IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.ModifiedAt).IsRequired(false);

        builder.HasIndex(t => new { t.DoctorId, t.Date, t.StartTime })
            .IsUnique()
            .HasDatabaseName("IX_AvailableTimeSlots_DoctorId_Date_StartTime");
        builder.HasIndex(t => t.DoctorId).HasDatabaseName("IX_AvailableTimeSlots_DoctorId");
        builder.HasIndex(t => t.Date).HasDatabaseName("IX_AvailableTimeSlots_Date");
        builder.HasIndex(t => t.IsBooked).HasDatabaseName("IX_AvailableTimeSlots_IsBooked");
        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasOne(t => t.Doctor)
            .WithMany(d => d.AvailableTimeSlots)
            .HasForeignKey(t => t.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
