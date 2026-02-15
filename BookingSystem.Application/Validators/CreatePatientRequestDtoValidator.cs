using BookingSystem.Application.DTOs.Patient;
using FluentValidation;

namespace BookingSystem.Application.Validators;

public class CreatePatientRequestDtoValidator : AbstractValidator<CreatePatientRequestDto>
{
    public CreatePatientRequestDtoValidator()
    {
        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .WithMessage("Date of birth is required")
            .LessThan(DateTime.UtcNow)
            .WithMessage("Date of birth must be in the past")
            .Must(dob => DateTime.UtcNow.Year - dob.Year <= 120)
            .WithMessage("Date of birth is invalid");

        RuleFor(x => x.Gender)
            .IsInEnum()
            .WithMessage("Invalid gender value");

        RuleFor(x => x.BloodGroup)
            .MaximumLength(10)
            .WithMessage("Blood group cannot exceed 10 characters")
            .When(x => !string.IsNullOrEmpty(x.BloodGroup));

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .WithMessage("Address cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.EmergencyContact)
            .MaximumLength(20)
            .WithMessage("Emergency contact cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.EmergencyContact));

        RuleFor(x => x.MedicalHistory)
            .MaximumLength(2000)
            .WithMessage("Medical history cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.MedicalHistory));
    }
}
