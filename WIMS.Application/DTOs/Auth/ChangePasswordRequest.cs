namespace WIMS.Application.DTOs.Auth;

public class ChangePasswordRequest
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
    public int UserId { get; set; }
}
