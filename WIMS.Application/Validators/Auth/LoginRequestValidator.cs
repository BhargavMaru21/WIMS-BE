using FluentValidation;
using WIMS.Application.DTOs.Auth;

namespace WIMS.Application.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
             .NotEmpty().WithMessage("Email is required.")
              .Matches(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$").WithMessage("Invalid email format.")
             .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"(?=.*[a-z])").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"(?=.*[A-Z])").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"(?=.*\d)").WithMessage("Password must contain at least one number.")
            .Matches(@"(?=.*[\W_])").WithMessage("Password must contain at least one special character.");
    }
}
