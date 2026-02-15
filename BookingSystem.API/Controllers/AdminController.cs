using BookingSystem.Application.DTOs.Admin;
using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BookingSystem.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet("statistics")]
    [SwaggerOperation(
        Summary = "Get system statistics (Admin)",
        Description = "Retrieves system-wide statistics including counts of users, doctors, patients, appointments, and clinics. " +
                      "Only accessible to users with the Admin role. Requires a valid JWT token in the Authorization header."
    )]
    [SwaggerResponse(200, "Statistics retrieved", typeof(ApiResponse<StatisticsDto>))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<ActionResult<ApiResponse<StatisticsDto>>> GetStatistics()
    {
        var result = await _adminService.GetStatisticsAsync();
        return Ok(ApiResponse<StatisticsDto>.SuccessResponse(result, "Statistics retrieved."));
    }
}
