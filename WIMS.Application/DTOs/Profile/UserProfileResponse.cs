namespace WIMS.Application.DTOs.Profile;

public class UserProfileResponse
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public required string Role { get; set; }
    public required string Status { get; set; }
    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}