namespace HospitalManagementSystem.Models;

public sealed class Doctor
{
    public int DoctorId { get; set; }
    public string DoctorCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Specialization { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public decimal ConsultationFee { get; set; }
    public bool IsAvailable { get; set; } = true;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
