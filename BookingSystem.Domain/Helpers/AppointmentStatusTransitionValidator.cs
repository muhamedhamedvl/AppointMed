using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Exceptions;

namespace BookingSystem.Domain.Helpers;

public static class AppointmentStatusTransitionValidator
{
    private static readonly Dictionary<AppointmentStatus, HashSet<AppointmentStatus>> AllowedTransitions = new()
    {
        [AppointmentStatus.Pending] = new HashSet<AppointmentStatus> 
        { 
            AppointmentStatus.Confirmed, 
            AppointmentStatus.Canceled 
        },
        [AppointmentStatus.Confirmed] = new HashSet<AppointmentStatus> 
        { 
            AppointmentStatus.Completed, 
            AppointmentStatus.Canceled,
            AppointmentStatus.NoShow
        },
        [AppointmentStatus.Completed] = new HashSet<AppointmentStatus>(), // Terminal state
        [AppointmentStatus.Canceled] = new HashSet<AppointmentStatus>(), // Terminal state
        [AppointmentStatus.NoShow] = new HashSet<AppointmentStatus>() // Terminal state
    };

    public static bool IsTransitionAllowed(AppointmentStatus currentStatus, AppointmentStatus newStatus)
    {
        if (currentStatus == newStatus)
            return true; // No-op transition is allowed

        return AllowedTransitions.TryGetValue(currentStatus, out var allowedStatuses) 
            && allowedStatuses.Contains(newStatus);
    }

    public static void ValidateTransition(AppointmentStatus currentStatus, AppointmentStatus newStatus)
    {
        if (!IsTransitionAllowed(currentStatus, newStatus))
        {
            var allowedStatuses = AllowedTransitions.TryGetValue(currentStatus, out var statuses) 
                ? string.Join(", ", statuses) 
                : "none (terminal state)";
            
            throw new InvalidStatusTransitionException(
                $"Invalid status transition from {currentStatus} to {newStatus}. " +
                $"Allowed transitions from {currentStatus}: {allowedStatuses}");
        }
    }
}
