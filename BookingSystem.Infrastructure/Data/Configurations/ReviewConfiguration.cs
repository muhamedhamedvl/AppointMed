using BookingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Infrastructure.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(2000);
        builder.Property(r => r.RowVersion).IsRowVersion().IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.ModifiedAt).IsRequired(false);

        builder.HasIndex(r => r.AppointmentId).IsUnique().HasDatabaseName("IX_Reviews_AppointmentId");
        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasOne(r => r.Appointment)
            .WithOne(a => a.Review)
            .HasForeignKey<Review>(r => r.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
