using HospitalManagementSystem.Models;

namespace HospitalManagementSystem.Services;

public static class AppointmentRules
{
    public const int DurationMinutes = 30;

    public static bool IsValidSlot(DateTime localTime) =>
        localTime.Minute % DurationMinutes == 0 && localTime.Second == 0 &&
        localTime.Millisecond == 0 && localTime.Ticks % TimeSpan.TicksPerMillisecond == 0;

    public static bool IsFutureSlot(DateTime localTime, DateTime nowUtc, IHospitalClock clock)
    {
        try { return clock.ToUtc(localTime) > DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc); }
        catch (ArgumentException) { return false; }
    }

    public static bool CanTransition(AppointmentStatus current, AppointmentStatus next) => current switch
    {
        AppointmentStatus.Scheduled => next is AppointmentStatus.Confirmed or AppointmentStatus.Completed or AppointmentStatus.Cancelled,
        AppointmentStatus.Confirmed => next is AppointmentStatus.Completed or AppointmentStatus.Cancelled,
        _ => false
    };
}
