using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Ocsp;
using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Admin;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Interfaces.Services.Admin;

namespace WIMS.Api.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IAdminUserManagementService _adminUserManagementService;

    public AdminController(IAdminUserManagementService adminUserManagementService)
    {
        _adminUserManagementService = adminUserManagementService;
    }

    [HttpPost("create-user")]
    public async Task<ApiResponse<UserResponseDto>> CreateUser(CreateUserRequest request)
    {
        var response = await _adminUserManagementService.CreateUser(request);
        return response;
    }

    [HttpGet("users")]
    public async Task<ApiResponse<PagedResult<UserSummaryResponse>>> GetUsers([FromQuery] QueryParameters qp)
    {
        var response = await _adminUserManagementService.GetUsers(qp);
        return response;
    }

    [HttpGet("users/{id:int}")]
    public async Task<ApiResponse<UserResponseDto>> GetUserById(int id)
    {
        var response = await _adminUserManagementService.GetUserById(id);
        return response;
    }

    [HttpDelete("users/{id:int}")]
    public async Task<ApiResponse<string>> DeleteUser(int id)
    {
        var response = await _adminUserManagementService.Deleteuser(id);
        return response;
    }

    [HttpPatch("users/{id:int}/status")]
    public async Task<ApiResponse<UserResponseDto>> UpdateUserStatus(int id, UpdateUserStatusRequest request)
    {
        var response = await _adminUserManagementService.UpdateUserStatus(id, request);
        return response;
    }

    [HttpPatch("users/{id:int}/role")]
    public async Task<ApiResponse<UserResponseDto>> UpdateUserRole(int id, UpdateUserRoleRequest request)
    {
        var response = await _adminUserManagementService.UpdateUserRole(id, request);
        return response;
    }

    [HttpPatch("users/{id:int}/warehouse")]
    public async Task<ApiResponse<UserResponseDto>> UpdateUserWarehouse(int id, UpdateUserWarehouseRequest request)
    {
        var response = await _adminUserManagementService.UpdateUserWarehouse(id, request);
        return response;
    }
}
