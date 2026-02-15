using BookingSystem.Application.DTOs.Doctor;
using FluentValidation;

namespace BookingSystem.Application.Validators;

public class OnboardDoctorRequestDtoValidator : AbstractValidator<OnboardDoctorRequestDto>
{
    public OnboardDoctorRequestDtoValidator()
    {
        RuleFor(x => x.Specialization)
            .NotEmpty()
            .WithMessage("Specialization is required")
            .MaximumLength(100)
            .WithMessage("Specialization cannot exceed 100 characters");

        RuleFor(x => x.LicenseNumber)
            .NotEmpty()
            .WithMessage("License number is required")
            .MaximumLength(50)
            .WithMessage("License number cannot exceed 50 characters");

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Years of experience must be 0 or greater")
            .LessThanOrEqualTo(70)
            .WithMessage("Years of experience cannot exceed 70");

        RuleFor(x => x.ConsultationFee)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Consultation fee must be 0 or greater")
            .LessThanOrEqualTo(100000)
            .WithMessage("Consultation fee cannot exceed 100,000");

        RuleFor(x => x.Bio)
            .MaximumLength(1000)
            .WithMessage("Bio cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Bio));

        RuleFor(x => x.ClinicId)
            .GreaterThan(0)
            .WithMessage("Clinic ID is required and must be greater than 0");
    }
}
