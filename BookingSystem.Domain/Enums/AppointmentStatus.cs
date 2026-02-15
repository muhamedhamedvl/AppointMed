namespace BookingSystem.Domain.Enums;

public enum AppointmentStatus
{
    Pending = 0,      // Just created, awaiting confirmation
    Confirmed = 1,    // Doctor confirmed
    Completed = 2,    // Appointment finished
    Canceled = 3,     // Canceled by patient or doctor
    NoShow = 4        // Patient didn't show up
}
