namespace HospitalManagementSystem.Models;

public sealed class Patient
{
    public int PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTime RegistrationDateUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
}
