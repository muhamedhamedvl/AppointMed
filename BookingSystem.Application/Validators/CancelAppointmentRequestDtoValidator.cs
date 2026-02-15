using BookingSystem.Application.DTOs.Appointment;
using FluentValidation;

namespace BookingSystem.Application.Validators;

public class CancelAppointmentRequestDtoValidator : AbstractValidator<CancelAppointmentRequestDto>
{
    public CancelAppointmentRequestDtoValidator()
    {
        RuleFor(x => x.CancellationReason)
            .MaximumLength(500)
            .WithMessage("Cancellation reason cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.CancellationReason));
    }
}
