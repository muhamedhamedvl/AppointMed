using BookingSystem.Application.DTOs.Auth;
using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BookingSystem.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[ApiExplorerSettings(GroupName = "Auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    [SwaggerOperation(
        Summary = "User registration",
        Description = "Creates a new user account and sends a 6-digit verification code to the provided email address. " +
                      "The verification code is valid for 24 hours. Users must verify their email before they can log in. " +
                      "Password must meet complexity requirements (minimum 6 characters, at least one uppercase letter, one lowercase letter, and one number)."
    )]
    [SwaggerResponse(200, "Registration successful. Verification email sent", typeof(ApiResponse<LoginResponseDto>))]
    [SwaggerResponse(400, "Registration failed. Email already exists or invalid data", typeof(ApiResponse<LoginResponseDto>))]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Signup([FromBody] RegisterRequestDto request)
    {
        var (success, message, data) = await _authService.SignupAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponse<LoginResponseDto>.FailureResponse(message));
        }

        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(data!, message));
    }

    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "User login",
        Description = "Authenticates a user with email and password. Returns a JWT token valid for 60 minutes. " +
                      "**IMPORTANT**: Users must verify their email address before they can log in. " +
                      "If the email is not verified, the login will fail with a 401 Unauthorized status. " +
                      "Check your email for the verification code after registration."
    )]
    [SwaggerResponse(200, "Login successful. JWT token returned", typeof(ApiResponse<LoginResponseDto>))]
    [SwaggerResponse(401, "Login failed. Invalid credentials or email not verified", typeof(ApiResponse<LoginResponseDto>))]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
    {
        var (success, message, data) = await _authService.LoginAsync(request);

        if (!success)
        {
            return Unauthorized(ApiResponse<LoginResponseDto>.FailureResponse(message));
        }

        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(data!, message));
    }

    [HttpPost("google-login")]
    [SwaggerOperation(
        Summary = "Google OAuth login",
        Description = "Authenticates a user using their Google account ID token. " +
                      "If the user doesn't exist, a new account is automatically created. " +
                      "Google-authenticated users have their email automatically verified and can log in immediately."
    )]
    [SwaggerResponse(200, "Google login successful. JWT token returned", typeof(ApiResponse<LoginResponseDto>))]
    [SwaggerResponse(400, "Google login failed. Invalid token or authentication error", typeof(ApiResponse<LoginResponseDto>))]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> GoogleLogin([FromBody] GoogleLoginRequestDto request)
    {
        var (success, message, data) = await _authService.GoogleLoginAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponse<LoginResponseDto>.FailureResponse(message));
        }

        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(data!, message));
    }

    [HttpGet("verify-account")]
    [SwaggerOperation(
        Summary = "Verify email with verification code",
        Description = "Verifies a user's email address using the 6-digit code sent during registration. " +
                      "The code is valid for 24 hours and can only be used once. " +
                      "After successful verification, the user can log in to their account. " +
                      "If the code has expired, use the resend-verification endpoint to request a new code."
    )]
    [SwaggerResponse(200, "Email verified successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Verification failed. Invalid or expired code", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> VerifyAccount(
        [FromQuery, SwaggerParameter("User's email address", Required = true)] string email,
        [FromQuery, SwaggerParameter("6-digit verification code sent to email", Required = true)] string code)
    {
        var request = new VerifyAccountRequestDto { Email = email, Code = code };
        var (success, message) = await _authService.VerifyAccountAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, message));
    }

    [HttpPost("resend-verification")]
    [SwaggerOperation(
        Summary = "Resend email verification code",
        Description = "Generates a new 6-digit verification code and sends it to the user's email address. " +
                      "The new code is valid for 24 hours and invalidates any previous codes. " +
                      "This endpoint includes rate limiting protection to prevent abuse. " +
                      "Use this if the original verification code has expired or was not received."
    )]
    [SwaggerResponse(200, "New verification code sent successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Resend failed. Email already verified or invalid request", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> ResendVerification([FromBody] ResendVerificationRequestDto request)
    {
        var (success, message) = await _authService.ResendVerificationCodeAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, message));
    }

    [HttpPost("forget-password")]
    [SwaggerOperation(
        Summary = "Request password reset",
        Description = "Initiates the password reset process by sending a reset token to the user's email address. " +
                      "For security reasons, this endpoint always returns success even if the email doesn't exist. " +
                      "The reset token can be used with the reset-password endpoint to set a new password."
    )]
    [SwaggerResponse(200, "Password reset email sent (if email exists)", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> ForgetPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var (success, message) = await _authService.ForgetPasswordAsync(request);

        return Ok(ApiResponse<object>.SuccessResponse(null, message));
    }

    [HttpPost("reset-password")]
    [SwaggerOperation(
        Summary = "Reset password with token",
        Description = "Resets the user's password using the token received via email from the forget-password endpoint. " +
                      "The token is single-use and expires after a certain period. " +
                      "The new password must meet complexity requirements (minimum 6 characters, with uppercase, lowercase, and number)."
    )]
    [SwaggerResponse(200, "Password reset successful", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Password reset failed. Invalid token or weak password", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var (success, message) = await _authService.ResetPasswordAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, message));
    }

    [HttpPatch("users/me/password")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Change password for authenticated user",
        Description = "Allows an authenticated user to change their password. " +
                      "Requires the current password for verification and a new password that meets complexity requirements. " +
                      "The user must be logged in (valid JWT token required) to use this endpoint."
    )]
    [SwaggerResponse(200, "Password changed successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Password change failed. Invalid current password or weak new password", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized. Invalid or missing JWT token", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.FailureResponse("User not authenticated"));
        }

        var (success, message) = await _authService.ChangePasswordAsync(userId, request);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, message));
    }

    [HttpPost("check-otp")]
    [SwaggerOperation(
        Summary = "Validate OTP code",
        Description = "Validates a one-time password (OTP) code sent to the user's email. " +
                      "OTP codes are typically used for two-factor authentication or additional security verification. " +
                      "The code expires after 5 minutes and can only be used once."
    )]
    [SwaggerResponse(200, "OTP verified successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "OTP verification failed. Invalid or expired code", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> CheckOtp([FromBody] CheckOtpRequestDto request)
    {
        var (success, message) = await _authService.CheckOtpAsync(request);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null, message));
    }

    [HttpPatch("logout")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Logout user",
        Description = "Logs out the current user. Since JWT tokens are stateless, logout is handled client-side by removing the token. " +
                      "This endpoint exists for consistency and can be extended with token blacklisting if needed in the future."
    )]
    [SwaggerResponse(200, "Logged out successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized. Invalid or missing JWT token", typeof(ApiResponse<object>))]
    public ActionResult<ApiResponse<object>> Logout()
    {
        // JWT is stateless, so logout is handled client-side by removing the token
        // This endpoint exists for consistency and can be extended with token blacklisting if needed
        return Ok(ApiResponse<object>.SuccessResponse(null, "Logged out successfully"));
    }
}
