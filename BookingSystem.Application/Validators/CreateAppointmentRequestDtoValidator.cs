using BookingSystem.Application.DTOs.Appointment;
using FluentValidation;

namespace BookingSystem.Application.Validators;

public class CreateAppointmentRequestDtoValidator : AbstractValidator<CreateAppointmentRequestDto>
{
    public CreateAppointmentRequestDtoValidator()
    {
        RuleFor(x => x.DoctorId)
            .GreaterThan(0)
            .WithMessage("Doctor ID must be greater than 0");

        RuleFor(x => x.TimeSlotId)
            .GreaterThan(0)
            .WithMessage("Time slot ID must be greater than 0");

        RuleFor(x => x.ReasonForVisit)
            .MaximumLength(500)
            .WithMessage("Reason for visit cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.ReasonForVisit));
    }
}
