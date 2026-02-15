using BookingSystem.Application.DTOs.Clinic;
using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BookingSystem.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ClinicsController : ControllerBase
{
    private readonly IClinicService _clinicService;
    private readonly ILogger<ClinicsController> _logger;

    public ClinicsController(IClinicService clinicService, ILogger<ClinicsController> logger)
    {
        _clinicService = clinicService;
        _logger = logger;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "List clinics",
        Description = "Retrieves a paginated list of clinics. Optional filter by city. " +
                      "Returns clinic details including name, address, and contact information. No authentication required."
    )]
    [SwaggerResponse(200, "Clinics retrieved", typeof(ApiResponse<PaginatedResult<ClinicDto>>))]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ClinicDto>>>> GetClinics(
        [FromQuery] string? city,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _clinicService.GetAllClinicsAsync(city, page, pageSize);
        return Ok(ApiResponse<PaginatedResult<ClinicDto>>.SuccessResponse(result, "Clinics retrieved."));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get clinic by ID",
        Description = "Retrieves detailed information for a single clinic by its ID, including address, contact, and associated doctors. " +
                      "No authentication required."
    )]
    [SwaggerResponse(200, "Clinic found", typeof(ApiResponse<ClinicDetailDto>))]
    [SwaggerResponse(404, "Clinic not found")]
    public async Task<ActionResult<ApiResponse<ClinicDetailDto>>> GetClinicDetails(int id)
    {
        var result = await _clinicService.GetClinicDetailsAsync(id);
        return Ok(ApiResponse<ClinicDetailDto>.SuccessResponse(result, "Clinic retrieved."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Create clinic (Admin)",
        Description = "Creates a new clinic in the system. Only accessible to users with the Admin role. " +
                      "Requires a valid JWT token. Request body must include name, address, city, and contact details."
    )]
    [SwaggerResponse(200, "Clinic created", typeof(ApiResponse<ClinicDto>))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<ApiResponse<ClinicDto>>> CreateClinic([FromBody] CreateClinicRequestDto request)
    {
        var result = await _clinicService.CreateClinicAsync(request);
        return Ok(ApiResponse<ClinicDto>.SuccessResponse(result, "Clinic created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Update clinic (Admin)",
        Description = "Updates an existing clinic by ID. Only accessible to users with the Admin role. " +
                      "Requires a valid JWT token in the Authorization header."
    )]
    [SwaggerResponse(200, "Clinic updated", typeof(ApiResponse<ClinicDto>))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<ApiResponse<ClinicDto>>> UpdateClinic(int id, [FromBody] UpdateClinicRequestDto request)
    {
        var result = await _clinicService.UpdateClinicAsync(id, request);
        return Ok(ApiResponse<ClinicDto>.SuccessResponse(result, "Clinic updated."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Delete clinic (Admin)",
        Description = "Permanently deletes a clinic by ID. Only accessible to users with the Admin role. " +
                      "Cannot delete a clinic that has doctors assigned. Requires a valid JWT token."
    )]
    [SwaggerResponse(204, "Clinic deleted")]
    [SwaggerResponse(400, "Cannot delete clinic with doctors")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> DeleteClinic(int id)
    {
        await _clinicService.DeleteClinicAsync(id);
        return NoContent();
    }
}
