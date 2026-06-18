using FluentValidation;
using WIMS.Application.CommonServices;
using WIMS.Application.DTOs.PurchaseOrder;
using WIMS.Domain.Enums;

namespace WIMS.Application.Validators.PurchaseOrder;

public class PoCreateRequestValidator : AbstractValidator<PoCreateRequest>
{
    public PoCreateRequestValidator()
    {
        RuleFor(x => x.SupplierName)
            .NotEmpty().WithMessage("Supplier name is required.")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Supplier Name can only contain letters and spaces.")
            .MinimumLength(2).WithMessage("Supplier name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Supplier name must not exceed 200 characters.");

        RuleFor(x => x.SupplierContact)
           .Matches(@"^(?:\+91[\-\s]?)?[6-9]\d{9}$").WithMessage("Supplier contact must be a valid Indian phone number.")
           .When(x => !string.IsNullOrWhiteSpace(x.SupplierContact));

        RuleFor(x => x.ExpectedDelivery)
            .NotEmpty().WithMessage("Expected Delivery date not empty")
            .GreaterThan(DateOnly.FromDateTime(HelperService.ToIST(DateTime.UtcNow))).WithMessage("Expected delivery date must be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}

public class PoUpdateRequestValidator : AbstractValidator<PoUpdateRequest>
{
    public PoUpdateRequestValidator()
    {
        RuleFor(x => x.SupplierName)
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Supplier Name can only contain letters and spaces.")
            .MinimumLength(2).WithMessage("Supplier name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Supplier name must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.SupplierName));

        RuleFor(x => x.SupplierContact)
            .Matches(@"^(?:\+91[\-\s]?)?[6-9]\d{9}$").WithMessage("Supplier contact must be a valid Indian phone number.")
            .When(x => !string.IsNullOrWhiteSpace(x.SupplierContact));

        RuleFor(x => x.ExpectedDelivery)
            .GreaterThan(DateOnly.FromDateTime(HelperService.ToIST(DateTime.UtcNow))).WithMessage("Expected delivery date must be in the future.")
            .When(x => x.ExpectedDelivery.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}

public class PoItemCreateRequestValidator : AbstractValidator<PoItemCreateRequest>
{
    public PoItemCreateRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("A valid ProductId is required.");

        RuleFor(x => x.OrderedQty)
            .GreaterThan(0).WithMessage("Ordered quantity must be greater than zero.");
    }
}

public class PoItemUpdateRequestValidator : AbstractValidator<PoItemUpdateRequest>
{
    public PoItemUpdateRequestValidator()
    {
        RuleFor(x => x.OrderedQty)
            .GreaterThan(0).WithMessage("Ordered quantity must be greater than zero.")
            .When(x => x.OrderedQty.HasValue);
    }
}

public class PoStatusUpdateRequestValidator : AbstractValidator<PoStatusUpdateRequest>
{
    public PoStatusUpdateRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(status => status == PoStatus.Submitted ||
                            status == PoStatus.Approved ||
                            status == PoStatus.Rejected ||
                            status == PoStatus.Cancelled)
            .WithMessage("Status must be Submitted,Approved,Rejected,Cancelled");

        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("Rejection reason is required")
            .MinimumLength(5).WithMessage("Rejection reason must be at least 5 characters.")
            .MaximumLength(500).WithMessage("Rejection reason must not exceed 500 characters.")
            .When(x => x.Status == PoStatus.Rejected);
    }
}
