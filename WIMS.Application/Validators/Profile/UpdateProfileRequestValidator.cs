using FluentValidation;
using WIMS.Application.DTOs.Profile;

namespace WIMS.Application.Validators.Profile;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters long.")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Full name can only contain letters and spaces.")
            .MaximumLength(150).WithMessage("Full name cannot exceed 150 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(?:\+91[\-\s]?)?[6-9]\d{9}$")
            .WithMessage("Phone number must be a valid Indian phone number.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
