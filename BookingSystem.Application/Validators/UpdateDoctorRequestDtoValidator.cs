using BookingSystem.Application.DTOs.Doctor;
using FluentValidation;

namespace BookingSystem.Application.Validators;

public class UpdateDoctorRequestDtoValidator : AbstractValidator<UpdateDoctorRequestDto>
{
    public UpdateDoctorRequestDtoValidator()
    {
        RuleFor(x => x.Specialization)
            .MaximumLength(100).WithMessage("Specialization must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Specialization));

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0).WithMessage("Years of experience must be 0 or greater")
            .LessThanOrEqualTo(70).WithMessage("Years of experience must not exceed 70")
            .When(x => x.YearsOfExperience.HasValue);

        RuleFor(x => x.ConsultationFee)
            .GreaterThan(0).WithMessage("Consultation fee must be greater than 0")
            .LessThanOrEqualTo(100000).WithMessage("Consultation fee must not exceed 100,000")
            .When(x => x.ConsultationFee.HasValue);

        RuleFor(x => x.Bio)
            .MaximumLength(1000).WithMessage("Bio must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Bio));

        RuleFor(x => x.ClinicId)
            .GreaterThan(0).WithMessage("Clinic ID must be greater than 0")
            .When(x => x.ClinicId.HasValue);
    }
}
