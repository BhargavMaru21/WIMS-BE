using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Auth;
using WIMS.Application.Interfaces.Services.Auth;

namespace WIMS.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }


    [HttpPost("login")]
    public async Task<ApiResponse<LoginResponse>> Login(LoginRequest request)
    {
        var response = await _authService.Login(request);
        return response;
    }

    [HttpPost("refresh-token")]
    public async Task<ApiResponse<LoginResponse>> RefreshToken()
    {
        var response = await _authService.RefreshToken();
        return response;
    }

    [HttpPost("logout")]
    public async Task<ApiResponse<string>> Logout()
    {
        var result = await _authService.Logout();
        return result;
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ApiResponse<string>> ChangePassword(ChangePasswordRequest request)
    {
        var result = await _authService.ChangePassword(request);
        return result;
    }

    [HttpPost("forgot-password")]
    public async Task<ApiResponse<string>> ForgotPassword(ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPassword(request);
        return result;
    }

    [HttpPost("reset-password")]
    public async Task<ApiResponse<string>> ResetPassword(ResetPasswordRequest request)
    {
        var result = await _authService.ResetPassword(request);
        return result;
    }

    [HttpPost("validate-link")]
    public async Task<ApiResponse<bool>> ValidateLink(ValidateLinkRequest request)
    {
        var result = await _authService.ValidateLink(request);
        return result;
    }

}
