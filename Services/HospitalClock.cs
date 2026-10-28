namespace HospitalManagementSystem.Services;

public interface IHospitalClock
{
    DateTime UtcNow { get; }
    DateTime ToLocal(DateTime utc);
    DateTime ToUtc(DateTime local);
    TimeZoneInfo TimeZone { get; }
}

public sealed class HospitalClock(IConfiguration configuration) : IHospitalClock
{
    public TimeZoneInfo TimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById(
        configuration["Hospital:TimeZoneId"] ?? "Asia/Kolkata");

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime ToLocal(DateTime utc)
    {
        var utcValue = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utcValue, TimeZone);
    }

    public DateTime ToUtc(DateTime local)
    {
        var localValue = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (TimeZone.IsInvalidTime(localValue))
            throw new ArgumentException("The selected local time does not exist in the configured time zone.", nameof(local));

        var offset = TimeZone.IsAmbiguousTime(localValue)
            ? TimeZone.GetAmbiguousTimeOffsets(localValue).Min()
            : TimeZone.GetUtcOffset(localValue);
        return new DateTimeOffset(localValue, offset).UtcDateTime;
    }
}
