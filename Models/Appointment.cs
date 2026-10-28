namespace HospitalManagementSystem.Models;

public enum AppointmentStatus
{
    Scheduled,
    Confirmed,
    Completed,
    Cancelled
}

public sealed class Appointment
{
    public int AppointmentId { get; set; }
    public string AppointmentCode { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public DateTime AppointmentDateTimeUtc { get; set; }
    public string ReasonForVisit { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public DateTime CreatedAtUtc { get; set; }
    public Bill? Bill { get; set; }
}
