using FluentValidation;
using WIMS.Application.DTOs.Admin;
using WIMS.Domain.Enums;

namespace WIMS.Application.Validators.Admin;

public class UpdateUserStatusRequestValidator : AbstractValidator<UpdateUserStatusRequest>
{
    public UpdateUserStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value.")
            .Must(s => s == UserStatus.Active || s == UserStatus.Inactive)
            .WithMessage("Only Active or Inactive status can be set. Locked is managed by the system.");
    }
}