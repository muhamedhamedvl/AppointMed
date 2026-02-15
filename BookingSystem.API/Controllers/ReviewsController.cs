using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.DTOs.Review;
using BookingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace BookingSystem.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IReviewService reviewService, ILogger<ReviewsController> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Submit review (Patient, completed appointments only)",
        Description = "Submits a review for a completed appointment. Only the patient who had the appointment can submit a review, and only once per appointment. " +
                      "Requires a valid JWT token. Request must include appointment ID, rating, and optional comment."
    )]
    [SwaggerResponse(200, "Review submitted", typeof(ApiResponse<ReviewDto>))]
    [SwaggerResponse(400, "Appointment not completed or already reviewed")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> CreateReview([FromBody] CreateReviewRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<ReviewDto>.FailureResponse("User not authenticated"));

        var result = await _reviewService.CreateReviewAsync(userId, request);
        return Ok(ApiResponse<ReviewDto>.SuccessResponse(result, "Review submitted successfully."));
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get reviews by doctor ID",
        Description = "Retrieves a paginated list of reviews for a specific doctor. " +
                      "Use query parameter doctorId. No authentication required."
    )]
    [SwaggerResponse(200, "Reviews retrieved", typeof(ApiResponse<PaginatedResult<ReviewDto>>))]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ReviewDto>>>> GetDoctorReviews(
        [FromQuery] int doctorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _reviewService.GetDoctorReviewsAsync(doctorId, page, pageSize);
        return Ok(ApiResponse<PaginatedResult<ReviewDto>>.SuccessResponse(result, "Reviews retrieved."));
    }

    [HttpGet("appointment/{appointmentId:int}")]
    [SwaggerOperation(
        Summary = "Get review by appointment ID",
        Description = "Retrieves the review associated with a specific appointment, if one exists. " +
                      "Returns 404 if no review has been submitted for that appointment. Requires a valid JWT token."
    )]
    [SwaggerResponse(200, "Review found", typeof(ApiResponse<ReviewDto>))]
    [SwaggerResponse(404, "No review found")]
    public async Task<ActionResult<ApiResponse<ReviewDto?>>> GetAppointmentReview(int appointmentId)
    {
        var result = await _reviewService.GetAppointmentReviewAsync(appointmentId);
        if (result == null)
            return NotFound(ApiResponse<ReviewDto?>.FailureResponse("No review found for this appointment."));
        return Ok(ApiResponse<ReviewDto?>.SuccessResponse(result, "Review retrieved."));
    }
}
