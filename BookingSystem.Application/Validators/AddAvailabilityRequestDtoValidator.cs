using BookingSystem.Application.DTOs.TimeSlot;
using FluentValidation;

namespace BookingSystem.Application.Validators;

public class AddAvailabilityRequestDtoValidator : AbstractValidator<AddAvailabilityRequestDto>
{
    public AddAvailabilityRequestDtoValidator()
    {
        RuleFor(x => x.Slots)
            .NotEmpty()
            .WithMessage("At least one time slot is required")
            .Must(slots => slots.Count > 0)
            .WithMessage("At least one time slot is required");

        RuleForEach(x => x.Slots)
            .SetValidator(new TimeSlotInputDtoValidator());
    }
}

public class TimeSlotInputDtoValidator : AbstractValidator<TimeSlotInputDto>
{
    public TimeSlotInputDtoValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date is required")
            .Must(date => date >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date cannot be in the past");

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .WithMessage("Start time is required");

        RuleFor(x => x.EndTime)
            .NotEmpty()
            .WithMessage("End time is required")
            .Must((slot, endTime) => endTime > slot.StartTime)
            .WithMessage("End time must be after start time")
            .When(x => x.StartTime != default);
    }
}
