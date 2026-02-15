using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.DTOs.Review;
using BookingSystem.Application.Exceptions;
using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IUserInfoProvider _userInfoProvider;

    public ReviewService(
        IReviewRepository reviewRepository,
        IAppointmentRepository appointmentRepository,
        IDoctorRepository doctorRepository,
        IPatientRepository patientRepository,
        IUserInfoProvider userInfoProvider)
    {
        _reviewRepository = reviewRepository;
        _appointmentRepository = appointmentRepository;
        _doctorRepository = doctorRepository;
        _patientRepository = patientRepository;
        _userInfoProvider = userInfoProvider;
    }

    public async Task<ReviewDto> CreateReviewAsync(string userId, CreateReviewRequestDto request)
    {
        var patient = await _patientRepository.GetByUserIdAsync(userId);
        if (patient == null)
            throw new Exception("Patient profile not found");

        var appointment = await _appointmentRepository.GetByIdWithReviewAsync(request.AppointmentId);
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

        review = await _reviewRepository.AddAsync(review);

        var doctor = await _doctorRepository.GetByIdAsync(appointment.DoctorId);
        if (doctor != null)
        {
            var reviewsForDoctor = await _reviewRepository.GetReviewsForDoctorAsync(doctor.Id);
            var allReviews = reviewsForDoctor.Append(review).ToList();
            doctor.TotalReviews = allReviews.Count;
            doctor.AverageRating = (decimal)allReviews.Average(r => r.Rating);
            doctor.ModifiedAt = DateTime.UtcNow;
            await _doctorRepository.UpdateAsync(doctor);
        }

        await _reviewRepository.SaveChangesAsync();

        var patientUser = await _userInfoProvider.GetByIdAsync(patient.UserId);
        var patientName = patientUser != null ? $"{patientUser.FirstName} {patientUser.LastName}".Trim() : "Unknown";
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

    public async Task<PaginatedResult<ReviewDto>> GetDoctorReviewsAsync(int doctorId, int pageNumber = 1, int pageSize = 10)
    {
        var skip = (pageNumber - 1) * pageSize;
        var reviews = await _reviewRepository.GetByDoctorIdAsync(doctorId, skip, pageSize);
        var totalCount = await _reviewRepository.CountByDoctorIdAsync(doctorId);

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
        var review = await _reviewRepository.GetByAppointmentIdAsync(appointmentId);
        return review != null ? await MapToReviewDto(review) : null;
    }

    private async Task<ReviewDto> MapToReviewDto(Review review)
    {
        var appointment = review.Appointment ?? throw new InvalidOperationException("Appointment not found for review");
        var patient = appointment.Patient ?? await _patientRepository.GetByIdAsync(appointment.PatientId);
        var patientUser = patient != null ? await _userInfoProvider.GetByIdAsync(patient.UserId) : null;
        var patientName = patientUser != null ? $"{patientUser.FirstName} {patientUser.LastName}".Trim() : "Unknown";

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
