using System.ComponentModel.DataAnnotations;

namespace WIMS.Application.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",ErrorMessage = "Invalid email format.")]
    [MaxLength(200, ErrorMessage = "Email cannot exceed 200 characters.")]
    public required string Email {get; set;}
}
