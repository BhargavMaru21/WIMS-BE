using System.Text.Json;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Interfaces.Services.Audit;
using WIMS.Domain.Entity;

namespace WIMS.Application.Service.Audit;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditRepo;

    public AuditService(IAuditLogRepository auditRepo)
    {
        _auditRepo = auditRepo;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        string entityId,
        int? performedBy,
        object? previousValue = null,
        object? newValue = null)
    {
        var log = new AuditLog
        {
            ActionType = action,
            EntityName = entityName,
            EntityId = entityId,
            PerformedBy = performedBy,
            PerformedAt = DateTime.UtcNow,
            PreviousValue = previousValue is null ? null : JsonSerializer.Serialize(previousValue),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue),
        };

        await _auditRepo.CreateAsync(log);
    }
}