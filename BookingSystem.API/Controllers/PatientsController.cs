using BookingSystem.Application.DTOs.Patient;
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
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(IPatientService patientService, ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }

    [HttpPost("profile")]
    [SwaggerOperation(
        Summary = "Create patient profile",
        Description = "Creates a patient profile for the currently authenticated user. " +
                      "Request must include required patient details. User can have only one patient profile. Requires a valid JWT token."
    )]
    [SwaggerResponse(200, "Profile created", typeof(PatientProfileDto))]
    [SwaggerResponse(400, "Profile already exists or invalid data")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<PatientProfileDto>> CreateProfile([FromBody] CreatePatientRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            var result = await _patientService.CreatePatientProfileAsync(userId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating patient profile");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("profile")]
    [SwaggerOperation(
        Summary = "Get my patient profile",
        Description = "Retrieves the patient profile of the currently authenticated user. " +
                      "Returns details such as name, date of birth, and contact. Requires a valid JWT token in the Authorization header."
    )]
    [SwaggerResponse(200, "Profile retrieved", typeof(PatientProfileDto))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Profile not found")]
    public async Task<ActionResult<PatientProfileDto>> GetMyProfile()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            var result = await _patientService.GetPatientProfileAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient profile");
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("profile")]
    [SwaggerOperation(
        Summary = "Update patient profile",
        Description = "Updates the patient profile of the currently authenticated user. " +
                      "Allows changing allowed profile fields. Requires a valid JWT token in the Authorization header."
    )]
    [SwaggerResponse(200, "Profile updated", typeof(PatientProfileDto))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Profile not found")]
    public async Task<ActionResult<PatientProfileDto>> UpdateProfile([FromBody] UpdatePatientRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            var result = await _patientService.UpdatePatientProfileAsync(userId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating patient profile");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Doctor")]
    [SwaggerOperation(
        Summary = "Get patient by ID",
        Description = "Retrieves a patient profile by ID. Only accessible to users with Admin or Doctor role. " +
                      "Requires a valid JWT token in the Authorization header."
    )]
    [SwaggerResponse(200, "Patient found", typeof(PatientProfileDto))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Patient not found")]
    public async Task<ActionResult<PatientProfileDto>> GetPatientById(int id)
    {
        try
        {
            var result = await _patientService.GetPatientByIdAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient by ID");
            return NotFound(new { error = ex.Message });
        }
    }
}
