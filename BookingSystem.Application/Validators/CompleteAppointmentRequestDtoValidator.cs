using BookingSystem.Application.DTOs.Appointment;
using FluentValidation;

namespace BookingSystem.Application.Validators;

public class CompleteAppointmentRequestDtoValidator : AbstractValidator<CompleteAppointmentRequestDto>
{
    public CompleteAppointmentRequestDtoValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .WithMessage("Notes cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
