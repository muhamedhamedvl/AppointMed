using FluentValidation;

namespace BookingSystem.Application.Validators;

public class SearchDoctorsQueryValidator : AbstractValidator<SearchDoctorsQuery>
{
    public SearchDoctorsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Page size must be at least 1")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100");

        RuleFor(x => x.MinFee)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum fee must be 0 or greater")
            .When(x => x.MinFee.HasValue);

        RuleFor(x => x.MaxFee)
            .GreaterThanOrEqualTo(0).WithMessage("Maximum fee must be 0 or greater")
            .When(x => x.MaxFee.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinFee.HasValue || !x.MaxFee.HasValue || x.MinFee.Value <= x.MaxFee.Value)
            .WithMessage("Minimum fee must be less than or equal to maximum fee")
            .When(x => x.MinFee.HasValue && x.MaxFee.HasValue);

        RuleFor(x => x.MinRating)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum rating must be between 0 and 5")
            .LessThanOrEqualTo(5).WithMessage("Minimum rating must be between 0 and 5")
            .When(x => x.MinRating.HasValue);

        RuleFor(x => x.Specialization)
            .MaximumLength(100).WithMessage("Specialization must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Specialization));

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.City));
    }
}

// Query model for validation
public class SearchDoctorsQuery
{
    public string? Specialization { get; set; }
    public string? Name { get; set; }
    public int? ClinicId { get; set; }
    public string? City { get; set; }
    public decimal? MinFee { get; set; }
    public decimal? MaxFee { get; set; }
    public decimal? MinRating { get; set; }
    public DateOnly? Date { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
