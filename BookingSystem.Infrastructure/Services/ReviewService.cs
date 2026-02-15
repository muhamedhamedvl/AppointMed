using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.DTOs.Review;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Exceptions;
using BookingSystem.Infrastructure.Data;
using BookingSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<ReviewDto> CreateReviewAsync(string userId, CreateReviewRequestDto request)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null)
            throw new Exception("Patient profile not found");

        var appointment = await _context.Appointments
            .Include(a => a.Review)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId);

        if (appointment == null)
            throw new Exception("Appointment not found");

        if (appointment.PatientId != patient.Id)
            throw new Exception("You can only review your own appointments");

        if (appointment.Status != AppointmentStatus.Completed)
            throw new BusinessRuleException("Reviews can only be submitted for completed appointments.");

        if (appointment.Review != null)
            throw new BusinessRuleException("This appointment has already been reviewed.");

        var review = new Review
        {
            AppointmentId = request.AppointmentId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        _context.Reviews.Add(review);

        var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);
        if (doctor != null)
        {
            var reviewsForDoctor = await _context.Reviews
                .Where(r => r.Appointment.DoctorId == doctor.Id)
                .ToListAsync();
            reviewsForDoctor.Add(review);

            doctor.TotalReviews = reviewsForDoctor.Count;
            doctor.AverageRating = (decimal)reviewsForDoctor.Average(r => r.Rating);
            doctor.ModifiedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return await MapToReviewDto(review);
    }

    public async Task<PaginatedResult<ReviewDto>> GetDoctorReviewsAsync(int doctorId, int pageNumber, int pageSize)
    {
        var query = _context.Reviews
            .Include(r => r.Appointment)
            .Where(r => r.Appointment.DoctorId == doctorId)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var reviews = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = new List<ReviewDto>();
        foreach (var review in reviews)
        {
            dtos.Add(await MapToReviewDto(review));
        }

        return new PaginatedResult<ReviewDto>
        {
            Data = dtos,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<ReviewDto?> GetAppointmentReviewAsync(int appointmentId)
    {
        var review = await _context.Reviews
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);

        return review != null ? await MapToReviewDto(review) : null;
    }

    private async Task<ReviewDto> MapToReviewDto(Review review)
    {
        var appointment = review.Appointment ?? await _context.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == review.AppointmentId);

        if (appointment == null)
            throw new InvalidOperationException("Appointment not found for review");

        var patient = appointment.Patient ?? await _context.Patients.FindAsync(appointment.PatientId);
        var patientUser = patient != null ? await _userManager.FindByIdAsync(patient.UserId) : null;
        var patientName = patientUser != null
            ? $"{patientUser.FirstName} {patientUser.LastName}".Trim()
            : "Unknown";

        return new ReviewDto
        {
            Id = review.Id,
            AppointmentId = review.AppointmentId,
            PatientId = appointment.PatientId,
            PatientName = patientName,
            DoctorId = appointment.DoctorId,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };
    }
}
