using BookingSystem.Application.DTOs.Auth;

namespace BookingSystem.Application.Interfaces.Services;

public interface IAuthService
{
    Task<(bool Success, string Message, LoginResponseDto? Data)> SignupAsync(RegisterRequestDto request);
    Task<(bool Success, string Message, LoginResponseDto? Data)> LoginAsync(LoginRequestDto request);
    Task<(bool Success, string Message, LoginResponseDto? Data)> GoogleLoginAsync(GoogleLoginRequestDto request);
    Task<(bool Success, string Message)> ForgetPasswordAsync(ForgotPasswordRequestDto request);
    Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<(bool Success, string Message)> ChangePasswordAsync(string userId, ChangePasswordRequestDto request);
    Task<(bool Success, string Message)> VerifyAccountAsync(VerifyAccountRequestDto request);
    Task<(bool Success, string Message)> CheckOtpAsync(CheckOtpRequestDto request);
    Task<(bool Success, string Message)> ResendVerificationCodeAsync(ResendVerificationRequestDto request);
}
