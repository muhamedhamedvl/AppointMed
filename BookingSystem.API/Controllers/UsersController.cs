using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.DTOs.User;
using BookingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BookingSystem.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[ApiExplorerSettings(GroupName = "User")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get current user profile",
        Description = "Retrieves the profile information of the currently authenticated user. " +
                      "Requires a valid JWT token in the Authorization header. " +
                      "Returns user details including name, email, phone number, and account creation date."
    )]
    [SwaggerResponse(200, "User profile retrieved successfully", typeof(ApiResponse<UserResponseDto>))]
    [SwaggerResponse(401, "Unauthorized. Invalid or missing JWT token", typeof(ApiResponse<UserResponseDto>))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse<UserResponseDto>))]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetMyData()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized(ApiResponse<UserResponseDto>.FailureResponse("User not authenticated"));
        }

        var (success, message, data) = await _userService.GetCurrentUserAsync(userId);

        if (!success)
        {
            return NotFound(ApiResponse<UserResponseDto>.FailureResponse(message));
        }

        return Ok(ApiResponse<UserResponseDto>.SuccessResponse(data!, message));
    }

    [HttpPatch("me")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Update current user profile",
        Description = "Updates the profile information of the currently authenticated user. " +
                      "Allows updating first name, last name, and phone number. " +
                      "Requires a valid JWT token. Email and password cannot be changed through this endpoint."
    )]
    [SwaggerResponse(200, "Profile updated successfully", typeof(ApiResponse<UserResponseDto>))]
    [SwaggerResponse(400, "Update failed. Invalid data provided", typeof(ApiResponse<UserResponseDto>))]
    [SwaggerResponse(401, "Unauthorized. Invalid or missing JWT token", typeof(ApiResponse<UserResponseDto>))]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> UpdateMyData([FromBody] UpdateMyProfileDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized(ApiResponse<UserResponseDto>.FailureResponse("User not authenticated"));
        }

        var (success, message, data) = await _userService.UpdateCurrentUserAsync(userId, dto);

        if (!success)
        {
            return BadRequest(ApiResponse<UserResponseDto>.FailureResponse(message));
        }

        return Ok(ApiResponse<UserResponseDto>.SuccessResponse(data!, message));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Get all users (Admin only)",
        Description = "Retrieves a paginated list of all users in the system. " +
                      "Only accessible to users with the Admin role. " +
                      "Supports pagination with configurable page number and page size. " +
                      "Returns user details including roles and account status."
    )]
    [SwaggerResponse(200, "Users retrieved successfully", typeof(ApiResponse<PaginatedResult<UserResponseDto>>))]
    [SwaggerResponse(400, "Invalid pagination parameters", typeof(ApiResponse<PaginatedResult<UserResponseDto>>))]
    [SwaggerResponse(401, "Unauthorized. Invalid or missing JWT token")]
    [SwaggerResponse(403, "Forbidden. Admin role required")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<UserResponseDto>>>> GetUsers(
        [FromQuery, SwaggerParameter("Page number (starting from 1)")] int page = 1,
        [FromQuery, SwaggerParameter("Number of items per page")] int pageSize = 10)
    {
        var (success, message, data) = await _userService.GetUsersAsync(page, pageSize);

        if (!success)
        {
            return BadRequest(ApiResponse<PaginatedResult<UserResponseDto>>.FailureResponse(message));
        }

        return Ok(ApiResponse<PaginatedResult<UserResponseDto>>.SuccessResponse(data!, message));
    }

    [HttpPost]
    [Authorize(Roles = "Worker,Admin")]
    [SwaggerOperation(
        Summary = "Create a new user (Worker/Admin only)",
        Description = "Creates a new user account in the system. " +
                      "Accessible to users with Worker or Admin roles. " +
                      "Allows setting user details including role assignment. " +
                      "The created user will need to verify their email before they can log in."
    )]
    [SwaggerResponse(201, "User created successfully", typeof(ApiResponse<UserResponseDto>))]
    [SwaggerResponse(400, "User creation failed. Email already exists or invalid data", typeof(ApiResponse<UserResponseDto>))]
    [SwaggerResponse(401, "Unauthorized. Invalid or missing JWT token")]
    [SwaggerResponse(403, "Forbidden. Worker or Admin role required")]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> CreateUser([FromBody] CreateUserDto dto)
    {
        var (success, message, data) = await _userService.CreateUserAsync(dto);

        if (!success)
        {
            return BadRequest(ApiResponse<UserResponseDto>.FailureResponse(message));
        }

        return Created($"/api/users/{data!.Id}", ApiResponse<UserResponseDto>.SuccessResponse(data, message));
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Update user data (Admin only)",
        Description = "Updates user information for any user in the system. " +
                      "Only accessible to users with the Admin role. " +
                      "Allows updating user details including role assignments and account status. " +
                      "Use this endpoint for administrative user management."
    )]
    [SwaggerResponse(200, "User updated successfully", typeof(ApiResponse<UserResponseDto>))]
    [SwaggerResponse(400, "Update failed. Invalid data or user not found", typeof(ApiResponse<UserResponseDto>))]
    [SwaggerResponse(401, "Unauthorized. Invalid or missing JWT token")]
    [SwaggerResponse(403, "Forbidden. Admin role required")]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> UpdateUser(
        [FromRoute, SwaggerParameter("User ID to update")] string id,
        [FromBody] UpdateUserDto dto)
    {
        var (success, message, data) = await _userService.UpdateUserAsync(id, dto);

        if (!success)
        {
            return BadRequest(ApiResponse<UserResponseDto>.FailureResponse(message));
        }

        return Ok(ApiResponse<UserResponseDto>.SuccessResponse(data!, message));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Delete user (Admin only)",
        Description = "Permanently deletes a user account from the system. " +
                      "Only accessible to users with the Admin role. " +
                      "This action is irreversible and will remove all user data. " +
                      "Use with caution. Consider deactivating users instead of deleting them when possible."
    )]
    [SwaggerResponse(200, "User deleted successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Deletion failed. User not found or cannot be deleted", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized. Invalid or missing JWT token")]
    [SwaggerResponse(403, "Forbidden. Admin role required")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(
        [FromRoute, SwaggerParameter("User ID to delete")] string id)
    {
        var (success, message) = await _userService.DeleteUserAsync(id);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, message));
    }
}
