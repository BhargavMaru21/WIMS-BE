namespace WIMS.Application.Interfaces.Services.Audit;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityName,
        string entityId,
        int? performedBy,
        object? previousValue = null,
        object? newValue = null
    );
}
