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

        var timeZoneId = OperatingSystem.IsWindows()
       ? "India Standard Time"
       : "Asia/Kolkata";

        TimeZoneInfo ist = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, ist);
    }
}
