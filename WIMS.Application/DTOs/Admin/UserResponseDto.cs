using WIMS.Domain.Enums;

namespace WIMS.Application.DTOs.Admin;

public class UserResponseDto
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public required UserRole Role { get; set; }
    public required UserStatus Status { get; set; }
    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
