using WIMS.Application.DTOs;
using WIMS.Application.DTOs.Admin;

namespace WIMS.Application.Interfaces.Services.Admin;

public interface IAdminUserManagementService
{
    Task<ApiResponse<UserResponseDto>> CreateUser(CreateUserRequest request);
    Task<ApiResponse<PagedResult<UserSummaryResponse>>> GetUsers(QueryParameters qp);
    Task<ApiResponse<UserResponseDto>> GetUserById(int userId);
    Task<ApiResponse<UserResponseDto>> UpdateUserStatus(int userId, UpdateUserStatusRequest request);
    Task<ApiResponse<UserResponseDto>> UpdateUserRole(int userId, UpdateUserRoleRequest request);
    Task<ApiResponse<UserResponseDto>> UpdateUserWarehouse(int userId, UpdateUserWarehouseRequest request);
    Task<ApiResponse<string>> Deleteuser(int userId);
}
