using FluentValidation;
using WIMS.Application.DTOs.UnitOfMeasure;

namespace WIMS.Application.Validators.UnitOfMeasure;

public class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
    public CreateUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Unit name is required.")
            .MinimumLength(2).WithMessage("Unit name must be at least 2 characters long.")
            .MaximumLength(100).WithMessage("Unit name must not exceed 100 characters.");

        RuleFor(x => x.Abbreviation)
            .NotEmpty().WithMessage("Abbreviation is required")
            .MaximumLength(10).WithMessage("Abbreviation name must not exceed 10 characters.");

    }
}
