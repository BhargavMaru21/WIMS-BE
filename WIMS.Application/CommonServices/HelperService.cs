using WIMS.Domain.Entity;

namespace WIMS.Application.CommonServices;

public static class HelperService
{
    public static void MarkDeleted(this ISoftDelete entity, int deletedByUserId)
    {
        entity.IsDeleted = true;
        entity.DeletedBy = deletedByUserId;
        entity.DeletedOn = DateTime.UtcNow;
    }

    public static DateTime ToIST(DateTime utcTime)
    {
        TimeZoneInfo ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, ist);
    }
}
