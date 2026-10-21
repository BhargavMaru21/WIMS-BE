using FluentValidation;
using WIMS.Application.DTOs.Auth;

namespace WIMS.Application.Validators.Auth;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
             .NotEmpty().WithMessage("Email is required.")
              .Matches(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$").WithMessage("Invalid email format.")
             .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required")
            .MaximumLength(500).WithMessage("Token cannot exceed 500 characters.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("NewPassword is required")
            .MinimumLength(8).WithMessage("NewPassword must be at least 8 characters.")
            .Matches(@"(?=.*[a-z])").WithMessage("NewPassword must contain at least one lowercase letter.")
            .Matches(@"(?=.*[A-Z])").WithMessage("NewPassword must contain at least one uppercase letter.")
            .Matches(@"(?=.*\d)").WithMessage("NewPassword must contain at least one number.")
            .Matches(@"(?=.*[\W_])").WithMessage("NewPassword must contain at least one special character.");
    }
}
public class ValidateLinkRequestValidator : AbstractValidator<ValidateLinkRequest>
{
    public ValidateLinkRequestValidator()
    {
        RuleFor(x => x.Email)
             .NotEmpty().WithMessage("Email is required.")
              .Matches(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$").WithMessage("Invalid email format.")
             .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required")
            .MaximumLength(500).WithMessage("Token cannot exceed 500 characters.");
    }
}
