using WIMS.Domain.Enums;

namespace WIMS.Domain.Entity;

public class User : SoftDeletableEntity
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public string? PhoneNumber { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public string? PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
}
