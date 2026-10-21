using WIMS.Domain.Enums;

namespace WIMS.Domain.Entity;

public class Warehouse : SoftDeletableEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required string City { get; set; }
    public required string ContactPerson { get; set; }
    public required string ContactPhone { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public ICollection<User> Users { get; set; } = [];
    public ICollection<Zone> Zones { get; set; } = [];
}
