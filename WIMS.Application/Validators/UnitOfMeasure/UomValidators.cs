using FluentValidation;
using WIMS.Application.DTOs.UnitOfMeasure;

namespace WIMS.Application.Validators.UnitOfMeasure;

public class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
   public CreateUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Unit name is required.")
            .Matches("^(?=.*[A-Za-z])[a-zA-Z ]+$").WithMessage("Unit name can only contain letters and spaces.")
            .MinimumLength(2).WithMessage("Unit name must be at least 2 characters.")
            .MaximumLength(50).WithMessage("Unit name must not exceed 50 characters.");
 
        RuleFor(x => x.Abbreviation)
            .NotEmpty().WithMessage("Abbreviation is required.")
            .Matches("^[a-zA-Z]+$").WithMessage("Abbreviation can only contain letters.")
            .MinimumLength(1).WithMessage("Abbreviation must be at least 1 character.")
            .MaximumLength(20).WithMessage("Abbreviation must not exceed 20 characters.");
    }
}
 
public class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2).WithMessage("Unit name must be at least 2 characters.")
            .MaximumLength(50).WithMessage("Unit name must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));
 
        RuleFor(x => x.Abbreviation)
            .MinimumLength(1).WithMessage("Abbreviation must be at least 1 character.")
            .MaximumLength(20).WithMessage("Abbreviation must not exceed 20 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Abbreviation));
    }
}
