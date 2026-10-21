using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WIMS.Application.Interfaces.Common;

namespace WIMS.Application.CommonServices;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    private ClaimsPrincipal User => _httpContextAccessor.HttpContext!.User;

    public int GetUserId()
    {
        int value = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return value;
    }

    public string GetUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role)!;
    }

    public int? GetWarehouseId()
    {
        var value = User.FindFirstValue("WarehouseId");
        int.TryParse(value, out int id);
        return id > 0 ? id : null;
    }
}