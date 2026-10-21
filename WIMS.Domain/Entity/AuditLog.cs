using WIMS.Domain.Enums;

namespace WIMS.Domain.Entity;

public class AuditLog:IEntity
{
    public int Id { get; set; }
    public required string ActionType { get; set; }
    public required string EntityName { get; set; } 
    public required string EntityId { get; set; }  
    public string? PreviousValue { get; set; }       
    public string? NewValue { get; set; }               
    public int? PerformedBy { get; set; }             
    public DateTime PerformedAt { get; set; }
    public User PerformedByUser { get; set; } = default!;
}