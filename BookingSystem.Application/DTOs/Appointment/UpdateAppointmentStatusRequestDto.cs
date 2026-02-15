namespace BookingSystem.Application.DTOs.Appointment;

/// <summary>
/// Request to update appointment status. Used for PATCH /appointments/{id}/status.
/// Allowed transitions: Pending→Confirmed/Cancelled, Confirmed→Completed/Cancelled/NoShow.
/// </summary>
public class UpdateAppointmentStatusRequestDto
{
    /// <summary>Target status: Pending, Confirmed, Completed, Cancelled, NoShow</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Optional notes (e.g. when completing the appointment).</summary>
    public string? Notes { get; set; }

    /// <summary>Required when status is Cancelled.</summary>
    public string? CancellationReason { get; set; }
}
