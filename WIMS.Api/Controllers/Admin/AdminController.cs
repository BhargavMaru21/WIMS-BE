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
    private int GetCurrentUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var response = await _adminUserManagementService.CreateUser(request, GetCurrentUserId());

        if (!response.IsSuccess)
        {
            return BadRequest(response);
        }

        return StatusCode(201, response);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] QueryParameters qp)
    {
        var response = await _adminUserManagementService.GetUsers(qp);
        return Ok(response);
    }

    [HttpGet("users/{id:int}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(ApiResponse<UserResponseDto>.Failure("Invalid user ID.", statusCode: 400));
        }

        var response = await _adminUserManagementService.GetUserById(id);

        return response.IsSuccess
            ? Ok(response)
            : NotFound(response);
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (id <= 0)
        {
            return BadRequest(ApiResponse<UserResponseDto>.Failure("Invalid user ID.", statusCode: 400));
        }

        var response = await _adminUserManagementService.Deleteuser(id, GetCurrentUserId());

        if (response.IsSuccess == false)
        {
            if (response.StatusCode == 404)
            {
                return NotFound(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        return Ok(response);
    }

    [HttpPatch("users/{id:int}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        int id, UpdateUserStatusRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(ApiResponse<UserResponseDto>.Failure("Invalid user ID.", statusCode: 400));
        }

        var response = await _adminUserManagementService.UpdateUserStatus(id, request, GetCurrentUserId());

        if (response.IsSuccess == false)
        {
            if (response.StatusCode == 404)
            {
                return NotFound(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        return Ok(response);

    }

    [HttpPatch("users/{id:int}/role")]
    public async Task<IActionResult> UpdateUserRole(
        int id, UpdateUserRoleRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(ApiResponse<UserResponseDto>.Failure("Invalid user ID.", statusCode: 400));
        }
        var response = await _adminUserManagementService.UpdateUserRole(id, request, GetCurrentUserId());

        if (response.IsSuccess == false)
        {
            if (response.StatusCode == 404)
            {
                return NotFound(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        return Ok(response);
    }

    [HttpPatch("users/{id:int}/warehouse")]
    public async Task<IActionResult> UpdateUserWarehouse(
        int id, UpdateUserWarehouseRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(ApiResponse<UserResponseDto>.Failure("Invalid user ID.", statusCode: 400));
        }
        var response = await _adminUserManagementService.UpdateUserWarehouse(id, request, GetCurrentUserId());

        if (response.IsSuccess == false)
        {
            if (response.StatusCode == 404)
            {
                return NotFound(response);
            }
            else
            {
                return BadRequest(response);
            }
        }

        return Ok(response);
    }
}
