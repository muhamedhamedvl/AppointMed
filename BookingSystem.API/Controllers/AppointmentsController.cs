using BookingSystem.Application.DTOs.Appointment;
using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.Enums;
using BookingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace BookingSystem.API.Controllers;

/// <summary>
/// Appointments: book, view, update status, cancel, reschedule.
/// Patient: book, view own, cancel, reschedule. Doctor: view own, update status. Admin: view all.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentService appointmentService,
        ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    /// <summary>Book a new appointment using an available time slot. Patient only. Email must be verified.</summary>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Book appointment",
        Description = "Books a new appointment using an available time slot. Available to patients only; email must be verified. " +
                      "Requires a valid JWT token. Returns the created appointment details including date, time, doctor, and clinic."
    )]
    [SwaggerResponse(200, "Appointment booked", typeof(ApiResponse<AppointmentDto>))]
    [SwaggerResponse(400, "Invalid request or slot unavailable")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> BookAppointment([FromBody] CreateAppointmentRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<AppointmentDto>.FailureResponse("User not authenticated"));

        var result = await _appointmentService.BookAppointmentAsync(userId, request);
        return Ok(ApiResponse<AppointmentDto>.SuccessResponse(result, "Appointment booked successfully."));
    }

    /// <summary>Get a single appointment by ID. Patient/Doctor/Admin only for their scope.</summary>
    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get appointment by ID",
        Description = "Retrieves a single appointment by its ID. Access is scoped: patients see their own, doctors see their appointments, admins see all. " +
                      "Requires a valid JWT token in the Authorization header."
    )]
    [SwaggerResponse(200, "Appointment found", typeof(ApiResponse<AppointmentDetailDto>))]
    [SwaggerResponse(404, "Appointment not found")]
    public async Task<ActionResult<ApiResponse<AppointmentDetailDto>>> GetAppointment(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<AppointmentDetailDto>.FailureResponse("User not authenticated"));

        var result = await _appointmentService.GetAppointmentByIdAsync(id, userId);
        return Ok(ApiResponse<AppointmentDetailDto>.SuccessResponse(result, "Appointment retrieved."));
    }

    /// <summary>Get appointments for the current user (patient or doctor). Supports status, upcoming, past, pagination.</summary>
    [HttpGet("my")]
    [SwaggerOperation(
        Summary = "Get my appointments",
        Description = "Retrieves appointments for the currently authenticated user (patient or doctor). " +
                      "Supports filtering by status, upcoming or past, and pagination (page, pageSize). Requires a valid JWT token."
    )]
    [SwaggerResponse(200, "List of appointments", typeof(ApiResponse<PaginatedResult<AppointmentDto>>))]
    public async Task<ActionResult<ApiResponse<PaginatedResult<AppointmentDto>>>> GetMyAppointments(
        [FromQuery] AppointmentStatus? status,
        [FromQuery] bool? upcoming,
        [FromQuery] bool? past,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<PaginatedResult<AppointmentDto>>.FailureResponse("User not authenticated"));

        var result = await _appointmentService.GetMyAppointmentsAsync(userId, status, upcoming, past, page, pageSize);
        return Ok(ApiResponse<PaginatedResult<AppointmentDto>>.SuccessResponse(result, "Appointments retrieved."));
    }

    /// <summary>Update appointment status (Confirmed, Completed, Cancelled, NoShow). Doctor for Confirm/Complete/NoShow; Patient/Doctor/Admin for Cancelled.</summary>
    [HttpPatch("{id:int}/status")]
    [SwaggerOperation(
        Summary = "Update appointment status",
        Description = "Updates the status of an appointment (e.g. Confirmed, Completed, Cancelled, NoShow). " +
                      "Doctors can confirm, complete, or mark NoShow; patients, doctors, or admins can cancel. Requires a valid JWT token."
    )]
    [SwaggerResponse(200, "Status updated", typeof(ApiResponse<AppointmentDto>))]
    [SwaggerResponse(400, "Invalid status or transition")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> UpdateAppointmentStatus(int id, [FromBody] UpdateAppointmentStatusRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<AppointmentDto>.FailureResponse("User not authenticated"));

        var result = await _appointmentService.UpdateAppointmentStatusAsync(id, userId, request);
        return Ok(ApiResponse<AppointmentDto>.SuccessResponse(result, "Appointment status updated."));
    }

    /// <summary>Cancel an appointment. Patient, Doctor, or Admin.</summary>
    [HttpPost("{id:int}/cancel")]
    [SwaggerOperation(
        Summary = "Cancel appointment",
        Description = "Cancels an existing appointment. Available to the patient, the assigned doctor, or an admin. " +
                      "Requires a valid JWT token. Optional reason can be provided in the request body."
    )]
    [SwaggerResponse(200, "Appointment cancelled", typeof(ApiResponse<AppointmentDto>))]
    [SwaggerResponse(400, "Cannot cancel")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> CancelAppointment(int id, [FromBody] CancelAppointmentRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<AppointmentDto>.FailureResponse("User not authenticated"));

        var result = await _appointmentService.CancelAppointmentAsync(id, userId, request);
        return Ok(ApiResponse<AppointmentDto>.SuccessResponse(result, "Appointment cancelled."));
    }

    /// <summary>Reschedule to another available slot. Patient only. New slot must be unbooked and same doctor.</summary>
    [HttpPost("{id:int}/reschedule")]
    [SwaggerOperation(
        Summary = "Reschedule appointment",
        Description = "Reschedules an appointment to another available time slot. Patients only. " +
                      "The new slot must be unbooked and for the same doctor. Requires a valid JWT token."
    )]
    [SwaggerResponse(200, "Appointment rescheduled", typeof(ApiResponse<AppointmentDto>))]
    [SwaggerResponse(400, "Slot not available or invalid")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> RescheduleAppointment(int id, [FromBody] RescheduleAppointmentRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<AppointmentDto>.FailureResponse("User not authenticated"));

        var result = await _appointmentService.RescheduleAppointmentAsync(id, userId, request);
        return Ok(ApiResponse<AppointmentDto>.SuccessResponse(result, "Appointment rescheduled."));
    }

    /// <summary>Get all appointments with filters. Admin only. Use query params for filters and pagination.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "List appointments (Admin)",
        Description = "Retrieves all appointments with optional filters (status, doctorId, patientId, startDate, endDate) and pagination. " +
                      "Only accessible to users with the Admin role. Requires a valid JWT token in the Authorization header."
    )]
    [SwaggerResponse(200, "Paginated appointments", typeof(ApiResponse<PaginatedResult<AppointmentDto>>))]
    public async Task<ActionResult<ApiResponse<PaginatedResult<AppointmentDto>>>> GetAppointments(
        [FromQuery] AppointmentStatus? status,
        [FromQuery] int? doctorId,
        [FromQuery] int? patientId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _appointmentService.GetAllAppointmentsAsync(status, doctorId, patientId, startDate, endDate, page, pageSize);
        return Ok(ApiResponse<PaginatedResult<AppointmentDto>>.SuccessResponse(result, "Appointments retrieved."));
    }
}
