namespace WIMS.Application.Interfaces.Common;

public interface ICurrentUserService
{
    int GetUserId();
    string GetUserRole();
    int? GetWarehouseId();
}