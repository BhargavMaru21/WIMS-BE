using FluentValidation;
using WIMS.Application.DTOs.Admin;
using WIMS.Domain.Enums;

namespace WIMS.Application.Validators.Admin;

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role value.")
            .Must(role => role == UserRole.WarehouseManager
                       || role == UserRole.StockKeeper
                       || role == UserRole.Viewer)
            .WithMessage("Only WarehouseManager, StockKeeper, or Viewer can be assigned.");
 
        // RuleFor(x => x.WarehouseId)
        //     .NotNull()
        //     .WithMessage("Warehouse is required for WarehouseManager and StockKeeper.")
        //     .GreaterThan(0)
        //     .WithMessage("Warehouse ID must be a valid positive number.")
        //     .When(x => x.Role == UserRole.WarehouseManager || x.Role == UserRole.StockKeeper);
 
        RuleFor(x => x.WarehouseId)
            .Null()
            .WithMessage("Viewer should not be assigned to a warehouse.")
            .When(x => x.Role == UserRole.Viewer);
    }
}
