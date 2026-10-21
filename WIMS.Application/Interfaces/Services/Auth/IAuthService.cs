using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Auth;

namespace WIMS.Application.Interfaces.Services.Auth;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> Login(LoginRequest request);
    Task<ApiResponse<LoginResponse>> RefreshToken();
    Task<ApiResponse<string>> ChangePassword(ChangePasswordRequest request);
    Task<ApiResponse<string>> ForgotPassword(ForgotPasswordRequest request);
    Task<ApiResponse<string>> ResetPassword(ResetPasswordRequest request);
    Task<ApiResponse<bool>> ValidateLink(ValidateLinkRequest request);
    Task<ApiResponse<string>> Logout();
}
