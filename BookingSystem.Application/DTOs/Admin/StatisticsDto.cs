namespace BookingSystem.Application.DTOs.Admin;

public class StatisticsDto
{
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalClinics { get; set; }
    public int TotalAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int ConfirmedAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CanceledAppointments { get; set; }
    public int TotalReviews { get; set; }
    public decimal AverageRating { get; set; }
}
