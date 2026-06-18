using FluentValidation;
using WIMS.Application.DTOs;

namespace WIMS.Application.Validators;


public class QueryParametersValidator : AbstractValidator<QueryParameters>
{
    private static readonly string[] AllowedSortFields =
    {
        "Id",
        "Name",
        "FullName",
        "Email",
        "CreatedAt",
        "UpdatedAt",
        "Code",
        "MaxCapacity",
        "Sku",
        "UnitPrice",
        "PoNumber",
        "SupplierName",
        "TotalAmount",
        "ExpectedDelivery",
        "OrderDate"
    };

    public QueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("PageNumber must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Search))
            .WithMessage("Search cannot exceed 200 characters.");

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage($"SortBy must be one of the following: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(x => x.SortDirection)
            .Must(sortDirection => sortDirection == "asc" || sortDirection == "desc")
            .WithMessage("Sort Direction must be asc or desc");


        RuleFor(x => x.Filters)
            .Must(filters => filters.Count <= 20)
            .WithMessage("A maximum of 20 filters are allowed.");

        RuleForEach(x => x.Filters)
            .ChildRules(filter =>
            {
                filter.RuleFor(x => x.Key)
                    .NotEmpty()
                    .MaximumLength(50)
                    .WithMessage("Filter key is required and cannot exceed 50 characters.");

                filter.RuleFor(x => x.Value)
                    .NotEmpty()
                    .MaximumLength(200)
                    .WithMessage("Filter value is required and cannot exceed 200 characters.");
            });
    }

    private static bool BeValidSortField(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return true;

        return AllowedSortFields.Contains(sortBy,StringComparer.OrdinalIgnoreCase);
    }
}