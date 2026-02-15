using BookingSystem.Domain.Base;

namespace BookingSystem.Domain.Entities;

public class Doctor : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? Bio { get; set; }
    public int ClinicId { get; set; }
    public bool IsApproved { get; set; }

    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Clinic Clinic { get; set; } = null!;
    public virtual ICollection<AvailableTimeSlot> AvailableTimeSlots { get; set; } = new List<AvailableTimeSlot>();
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
