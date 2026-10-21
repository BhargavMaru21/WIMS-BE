namespace WIMS.Domain.Entity;

public class SoftDeletableEntity : AuditableEntity, ISoftDelete
{
    public bool IsDeleted { get; set; } = false;
    public int? DeletedBy { get; set; }
    public DateTime? DeletedOn { get; set; }
}
