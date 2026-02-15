using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.DTOs.Doctor;
using BookingSystem.Application.DTOs.TimeSlot;
using BookingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace BookingSystem.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;
    private readonly ILogger<DoctorsController> _logger;

    public DoctorsController(IDoctorService doctorService, ILogger<DoctorsController> logger)
    {
        _doctorService = doctorService;
        _logger = logger;
    }

    /// <summary>Search doctors with filters and pagination. Public.</summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Search doctors",
        Description = "Searches doctors with optional filters: specialization, name, clinicId, city, fee range, minimum rating, and date for availability. " +
                      "Returns a paginated list. No authentication required."
    )]
    [SwaggerResponse(200, "Doctors retrieved", typeof(ApiResponse<PaginatedResult<DoctorProfileDto>>))]
    public async Task<ActionResult<ApiResponse<PaginatedResult<DoctorProfileDto>>>> SearchDoctors(
        [FromQuery] string? specialization,
        [FromQuery] string? name,
        [FromQuery] int? clinicId,
        [FromQuery] string? city,
        [FromQuery] decimal? minFee,
        [FromQuery] decimal? maxFee,
        [FromQuery] decimal? minRating,
        [FromQuery] DateOnly? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _doctorService.SearchDoctorsAsync(
            specialization, name, clinicId, city, minFee, maxFee, minRating, date, page, pageSize);
        return Ok(ApiResponse<PaginatedResult<DoctorProfileDto>>.SuccessResponse(result, "Doctors retrieved."));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get doctor by ID",
        Description = "Retrieves detailed profile information for a single doctor by ID, including specialization, clinic, rating, and contact. " +
                      "No authentication required."
    )]
    [SwaggerResponse(200, "Doctor found", typeof(ApiResponse<DoctorDetailDto>))]
    [SwaggerResponse(404, "Doctor not found")]
    public async Task<ActionResult<ApiResponse<DoctorDetailDto>>> GetDoctorDetails(int id)
    {
        var result = await _doctorService.GetDoctorDetailsAsync(id);
        return Ok(ApiResponse<DoctorDetailDto>.SuccessResponse(result, "Doctor retrieved."));
    }

    [HttpPost("onboard")]
    [Authorize(Roles = "User")]
    [SwaggerOperation(
        Summary = "Onboard as a doctor",
        Description = "Registers the currently authenticated user as a doctor. Requires User role. " +
                      "Request must include specialization, clinic, consultation fee, and other profile details. Requires a valid JWT token."
    )]
    [SwaggerResponse(200, "Doctor onboarded", typeof(ApiResponse<DoctorProfileDto>))]
    [SwaggerResponse(400, "Invalid request or user already a doctor")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<ApiResponse<DoctorProfileDto>>> OnboardDoctor([FromBody] OnboardDoctorRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<DoctorProfileDto>.FailureResponse("User not authenticated"));

        var result = await _doctorService.OnboardDoctorAsync(userId, request);
        return Ok(ApiResponse<DoctorProfileDto>.SuccessResponse(result, "Doctor onboarded successfully."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Doctor")]
    [SwaggerOperation(
        Summary = "Update doctor profile",
        Description = "Updates the profile of the authenticated doctor (specialization, fee, bio, etc.). " +
                      "Doctors can only update their own profile. Requires a valid JWT token."
    )]
    [SwaggerResponse(200, "Profile updated", typeof(ApiResponse<DoctorProfileDto>))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<ApiResponse<DoctorProfileDto>>> UpdateDoctor(int id, [FromBody] UpdateDoctorRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<DoctorProfileDto>.FailureResponse("User not authenticated"));

        var result = await _doctorService.UpdateDoctorAsync(id, userId, request);
        return Ok(ApiResponse<DoctorProfileDto>.SuccessResponse(result, "Profile updated."));
    }

    [HttpGet("{id:int}/availability")]
    [SwaggerOperation(
        Summary = "Get doctor availability",
        Description = "Retrieves available time slots for a doctor within the given date range (startDate, endDate). " +
                      "Returns list of slots that can be used for booking. No authentication required."
    )]
    [SwaggerResponse(200, "Availability retrieved", typeof(ApiResponse<List<AvailableTimeSlotDto>>))]
    public async Task<ActionResult<ApiResponse<List<AvailableTimeSlotDto>>>> GetDoctorAvailability(
        int id,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate)
    {
        var result = await _doctorService.GetDoctorAvailabilityAsync(id, startDate, endDate);
        return Ok(ApiResponse<List<AvailableTimeSlotDto>>.SuccessResponse(result, "Availability retrieved."));
    }

    [HttpPost("{id:int}/availability")]
    [Authorize(Roles = "Doctor")]
    [SwaggerOperation(
        Summary = "Add availability",
        Description = "Adds new availability time slots for the authenticated doctor. " +
                      "Doctors can only manage their own availability. Requires a valid JWT token."
    )]
    [SwaggerResponse(200, "Availability added", typeof(ApiResponse<List<AvailableTimeSlotDto>>))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<ApiResponse<List<AvailableTimeSlotDto>>>> AddAvailability(int id, [FromBody] AddAvailabilityRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<List<AvailableTimeSlotDto>>.FailureResponse("User not authenticated"));

        var result = await _doctorService.AddAvailabilityAsync(id, userId, request);
        return Ok(ApiResponse<List<AvailableTimeSlotDto>>.SuccessResponse(result, "Availability added."));
    }

    [HttpDelete("{doctorId:int}/availability/{slotId:int}")]
    [Authorize(Roles = "Doctor")]
    [SwaggerOperation(
        Summary = "Delete time slot",
        Description = "Removes a time slot from the doctor's availability. Cannot delete a slot that is already booked. " +
                      "Doctors can only delete their own slots. Requires a valid JWT token."
    )]
    [SwaggerResponse(204, "Time slot deleted")]
    [SwaggerResponse(400, "Cannot delete booked slot")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> DeleteTimeSlot(int doctorId, int slotId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _doctorService.DeleteTimeSlotAsync(doctorId, slotId, userId);
        return NoContent();
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Approve doctor (Admin)",
        Description = "Approves a pending doctor so they can accept appointments. Only accessible to users with the Admin role. " +
                      "Requires a valid JWT token in the Authorization header."
    )]
    [SwaggerResponse(200, "Doctor approved", typeof(ApiResponse<DoctorProfileDto>))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<ApiResponse<DoctorProfileDto>>> ApproveDoctor(int id)
    {
        var result = await _doctorService.ApproveDoctorAsync(id);
        return Ok(ApiResponse<DoctorProfileDto>.SuccessResponse(result, "Doctor approved."));
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Get pending doctors (Admin)",
        Description = "Retrieves a paginated list of doctors awaiting approval. Only accessible to users with the Admin role. " +
                      "Requires a valid JWT token in the Authorization header."
    )]
    [SwaggerResponse(200, "Pending doctors", typeof(ApiResponse<PaginatedResult<DoctorProfileDto>>))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<DoctorProfileDto>>>> GetPendingDoctors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _doctorService.GetPendingDoctorsAsync(page, pageSize);
        return Ok(ApiResponse<PaginatedResult<DoctorProfileDto>>.SuccessResponse(result, "Pending doctors retrieved."));
    }
}
