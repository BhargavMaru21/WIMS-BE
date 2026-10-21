namespace WIMS.Application.DTOs.Auth;

public class ValidateLinkRequest
{
    public required string Token {get; set;}
    public required string Email {get; set;}
}
