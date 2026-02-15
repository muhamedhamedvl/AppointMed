using BookingSystem.Application.DTOs.Auth;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace BookingSystem.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailQueueService _emailQueueService;
    private readonly IGoogleAuthService _googleAuthService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IEmailQueueService emailQueueService,
        IGoogleAuthService googleAuthService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _emailQueueService = emailQueueService;
        _googleAuthService = googleAuthService;
    }

    public async Task<(bool Success, string Message, LoginResponseDto? Data)> SignupAsync(RegisterRequestDto request)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return (false, "User with this email already exists", null);
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = false // Email not confirmed until verification
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Registration failed: {errors}", null);
        }

        // Assign default User role
        await _userManager.AddToRoleAsync(user, "User");

        // Generate 6-digit verification code
        var verificationCode = GenerateVerificationCode();
        user.VerificationCode = verificationCode;
        user.VerificationCodeExpiry = DateTime.UtcNow.AddHours(24);
        await _userManager.UpdateAsync(user);

        // Queue verification email for async processing
        await _emailQueueService.QueueVerificationEmailAsync(user.Email!, verificationCode, $"{user.FirstName} {user.LastName}");

        // Generate JWT token (user can get token but cannot use protected endpoints until verified)
        var roles = await _userManager.GetRolesAsync(user);
        var jwtToken = _tokenService.GenerateToken(user.Id, user.Email!, roles);

        var response = new LoginResponseDto
        {
            Token = jwtToken,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };

        return (true, "Registration successful. Please check your email to verify your account before logging in.", response);
    }

    public async Task<(bool Success, string Message, LoginResponseDto? Data)> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return (false, "Invalid email or password", null);
        }

        // Check if email is verified
        if (!user.EmailConfirmed)
        {
            return (false, "Please verify your email address before logging in. Check your inbox for the verification code.", null);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            return (false, "Invalid email or password", null);
        }

        // Generate token
        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user.Id, user.Email!, roles);

        var response = new LoginResponseDto
        {
            Token = token,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };

        return (true, "Login successful", response);
    }

    public async Task<(bool Success, string Message, LoginResponseDto? Data)> GoogleLoginAsync(GoogleLoginRequestDto request)
    {
        var (success, message, email, firstName, lastName) = await _googleAuthService.ValidateGoogleTokenAsync(request.IdToken);
        
        if (!success || string.IsNullOrEmpty(email))
        {
            return (false, message, null);
        }

        // Find or create user
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName ?? "",
                LastName = lastName ?? "",
                EmailConfirmed = true, // Google accounts are already verified
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return (false, "Failed to create user account", null);
            }

            await _userManager.AddToRoleAsync(user, "User");
        }
        else
        {
            // If user exists but signed up with regular auth, mark as verified
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                user.VerificationCode = null;
                user.VerificationCodeExpiry = null;
                await _userManager.UpdateAsync(user);
            }
        }

        // Generate JWT token
        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user.Id, user.Email!, roles);

        var response = new LoginResponseDto
        {
            Token = token,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };

        return (true, "Google login successful", response);
    }

    public async Task<(bool Success, string Message)> ForgetPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return (true, "If the email exists, a password reset link has been sent");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        await _emailQueueService.QueuePasswordResetEmailAsync(user.Email!, token);

        return (true, "If the email exists, a password reset link has been sent");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return (false, "Invalid request");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Password reset failed: {errors}");
        }

        return (true, "Password reset successful");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(string userId, ChangePasswordRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Password change failed: {errors}");
        }

        return (true, "Password changed successfully");
    }

    public async Task<(bool Success, string Message)> VerifyAccountAsync(VerifyAccountRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return (false, "Invalid verification request");
        }

        // Check if already verified
        if (user.EmailConfirmed)
        {
            return (false, "Email is already verified");
        }

        // Check if verification code exists
        if (string.IsNullOrEmpty(user.VerificationCode))
        {
            return (false, "No verification code found. Please request a new one");
        }

        // Check if code is expired
        if (user.VerificationCodeExpiry == null || user.VerificationCodeExpiry < DateTime.UtcNow)
        {
            return (false, "Verification code has expired. Please request a new one");
        }

        // Validate code
        if (user.VerificationCode != request.Code)
        {
            return (false, "Invalid verification code");
        }

        // Mark email as confirmed and clear verification code (single-use)
        user.EmailConfirmed = true;
        user.VerificationCode = null;
        user.VerificationCodeExpiry = null;
        await _userManager.UpdateAsync(user);

        return (true, "Email verified successfully. You can now log in");
    }

    public async Task<(bool Success, string Message)> CheckOtpAsync(CheckOtpRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return (false, "Invalid OTP request");
        }

        if (user.OtpCode == null || user.OtpExpiry == null)
        {
            return (false, "No OTP found for this user");
        }

        if (user.OtpExpiry < DateTime.UtcNow)
        {
            return (false, "OTP has expired");
        }

        if (user.OtpCode != request.OtpCode)
        {
            return (false, "Invalid OTP code");
        }

        // Clear OTP after successful validation
        user.OtpCode = null;
        user.OtpExpiry = null;
        await _userManager.UpdateAsync(user);

        return (true, "OTP verified successfully");
    }

    public async Task<(bool Success, string Message)> ResendVerificationCodeAsync(ResendVerificationRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist for security
            return (true, "If the email exists, a new verification code has been sent");
        }

        // Check if already verified
        if (user.EmailConfirmed)
        {
            return (false, "Email is already verified");
        }

        // Generate new verification code and invalidate old one
        var verificationCode = GenerateVerificationCode();
        user.VerificationCode = verificationCode;
        user.VerificationCodeExpiry = DateTime.UtcNow.AddHours(24);
        await _userManager.UpdateAsync(user);

        // Queue new verification email
        await _emailQueueService.QueueVerificationEmailAsync(user.Email!, verificationCode, $"{user.FirstName} {user.LastName}");

        return (true, "A new verification code has been sent to your email");
    }

    // Helper method to generate secure 6-digit verification code
    private string GenerateVerificationCode()
    {
        // Use cryptographically secure random number generator
        var randomNumber = RandomNumberGenerator.GetInt32(100000, 1000000);
        return randomNumber.ToString();
    }
}
