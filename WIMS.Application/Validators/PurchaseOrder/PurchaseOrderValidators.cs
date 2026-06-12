using FluentValidation;
using WIMS.Application.CommonServices;
using WIMS.Application.DTOs.PurchaseOrder;

namespace WIMS.Application.Validators.PurchaseOrder;

public class PurchaseOrderCreateRequestValidator : AbstractValidator<PurchaseOrderCreateRequest>
{
    public PurchaseOrderCreateRequestValidator()
    {
        RuleFor(x => x.SupplierName)
            .NotEmpty().WithMessage("Supplier Name is required")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Supplier Name can only contain letters and spaces.")
            .MinimumLength(2).WithMessage("Supplier Name must contain at least 2 characters")
            .MaximumLength(200).WithMessage("Supplier Name cannot exceed 150 characters.");

        RuleFor(x => x.SupplierContact)
            .Matches(@"^(?:\+91[\-\s]?)?[6-9]\d{9}$").WithMessage("Supplier contact must be a valid Indian phone number.")
            .When(x => !string.IsNullOrWhiteSpace(x.SupplierContact));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Note must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));

        RuleFor(x => x.ExpectedDelivery)
            .NotEmpty().WithMessage("Expected Delivery date not empty")
            .GreaterThan(DateOnly.FromDateTime(HelperService.ToIST(DateTime.UtcNow))).WithMessage("Expected Delivery date greater then today's date");
    }
}
