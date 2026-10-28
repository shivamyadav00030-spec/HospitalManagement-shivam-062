using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Models;

public sealed class Department
{
    public int DepartmentId { get; set; }

    [Required, StringLength(80)]
    public string DepartmentName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
