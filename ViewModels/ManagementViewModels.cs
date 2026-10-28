using System.ComponentModel.DataAnnotations;
using HospitalManagementSystem.Models;

namespace HospitalManagementSystem.ViewModels;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public string? Search { get; init; }
}

public sealed class PagerViewModel
{
    public string ControllerName { get; init; } = string.Empty;
    public int Page { get; init; }
    public int TotalPages { get; init; }
    public Dictionary<string, string?> RouteValues { get; init; } = new();
}

public sealed class DashboardViewModel
{
    public DateTime CurrentLocalTime { get; init; }
    public int PatientCount { get; init; }
    public int DoctorCount { get; init; }
    public int DepartmentCount { get; init; }
    public int AppointmentCount { get; init; }
    public int TodayAppointmentCount { get; init; }
    public int PendingAppointmentCount { get; init; }
    public int BillCount { get; init; }
    public IReadOnlyList<AppointmentRowViewModel> RecentAppointments { get; init; } = Array.Empty<AppointmentRowViewModel>();
}

public sealed class PatientFormViewModel
{
    public int PatientId { get; set; }

    [Required, StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [DataType(DataType.Date), Required]
    public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-25);

    [Required, StringLength(24)]
    public string Gender { get; set; } = string.Empty;

    [Required, StringLength(24), RegularExpression(@"^\+?[0-9\s()\-]{7,24}$", ErrorMessage = "Enter a valid phone number.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }

    [Required, StringLength(300, MinimumLength = 5)]
    public string Address { get; set; } = string.Empty;
    public IReadOnlyList<string> GenderOptions { get; set; } = Array.Empty<string>();
}

public sealed class PatientListItemViewModel
{
    public int PatientId { get; init; }
    public string PatientCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; }
}

public sealed class PatientDetailsViewModel
{
    public Patient Patient { get; init; } = new();
    public IReadOnlyList<AppointmentRowViewModel> RecentAppointments { get; init; } = Array.Empty<AppointmentRowViewModel>();
    public int BillCount { get; init; }
}

public sealed class DoctorFormViewModel
{
    public int DoctorId { get; set; }

    [Required, StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Select a department.")]
    public int DepartmentId { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Specialization { get; set; } = string.Empty;

    [Required, StringLength(24), RegularExpression(@"^\+?[0-9\s()\-]{7,24}$", ErrorMessage = "Enter a valid phone number.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }

    [Range(typeof(decimal), "0.00", "99999999.99", ErrorMessage = "Fee must be zero or greater.")]
    public decimal ConsultationFee { get; set; }
    public IReadOnlyList<LookupItem> Departments { get; set; } = Array.Empty<LookupItem>();
}

public sealed class DoctorListItemViewModel
{
    public int DoctorId { get; init; }
    public string DoctorCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Specialization { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public decimal ConsultationFee { get; init; }
    public bool IsAvailable { get; init; }
}

public sealed class DoctorDetailsViewModel
{
    public Doctor Doctor { get; init; } = new();
    public int UpcomingAppointmentCount { get; init; }
}

public sealed class DepartmentFormViewModel
{
    public int DepartmentId { get; set; }

    [Required, StringLength(80, MinimumLength = 2)]
    public string DepartmentName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public sealed class DepartmentListItemViewModel
{
    public int DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DoctorCount { get; init; }
    public bool IsActive { get; init; }
}

public sealed class DepartmentDetailsViewModel
{
    public Department Department { get; init; } = new();
    public IReadOnlyList<DoctorListItemViewModel> Doctors { get; init; } = Array.Empty<DoctorListItemViewModel>();
}

public sealed class AppointmentFormViewModel
{
    public int AppointmentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a patient.")]
    public int PatientId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a doctor.")]
    public int DoctorId { get; set; }

    [Required, DataType(DataType.DateTime)]
    public DateTime AppointmentDateTimeLocal { get; set; } = DateTime.Now.AddDays(1).Date.AddHours(9);

    [Required, StringLength(300, MinimumLength = 3)]
    public string ReasonForVisit { get; set; } = string.Empty;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public IReadOnlyList<LookupItem> Patients { get; set; } = Array.Empty<LookupItem>();
    public IReadOnlyList<LookupItem> Doctors { get; set; } = Array.Empty<LookupItem>();
    public string TimeZoneName { get; set; } = "Asia/Kolkata";
}

public sealed class AppointmentRowViewModel
{
    public int AppointmentId { get; init; }
    public string AppointmentCode { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string PatientCode { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public DateTime AppointmentDateTimeLocal { get; init; }
    public AppointmentStatus Status { get; init; }
}

public sealed class AppointmentIndexViewModel
{
    public PagedResult<AppointmentRowViewModel> Results { get; init; } = new();
    public AppointmentFilterViewModel Filter { get; init; } = new();
    public IReadOnlyList<LookupItem> Doctors { get; init; } = Array.Empty<LookupItem>();
    public IReadOnlyList<LookupItem> Patients { get; init; } = Array.Empty<LookupItem>();
    public string TimeZoneName { get; init; } = "Asia/Kolkata";
}

public sealed class AppointmentDetailsViewModel
{
    public Appointment Appointment { get; init; } = new();
    public DateTime AppointmentDateTimeLocal { get; init; }
    public string TimeZoneName { get; init; } = "Asia/Kolkata";
    public bool CanMarkCompleted { get; init; }
}

public sealed class RescheduleViewModel
{
    public int AppointmentId { get; set; }
    public string AppointmentCode { get; set; } = string.Empty;

    [Required, DataType(DataType.DateTime)]
    public DateTime AppointmentDateTimeLocal { get; set; }
    public string TimeZoneName { get; set; } = "Asia/Kolkata";
}

public sealed class AppointmentFilterViewModel
{
    public string? PatientSearch { get; set; }
    public int? DoctorId { get; set; }
    public DateOnly? Date { get; set; }
    public AppointmentStatus? Status { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class LookupItem
{
    public int Id { get; init; }
    public string Label { get; init; } = string.Empty;
}

public sealed class BillCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select an eligible appointment.")]
    public int AppointmentId { get; set; }

    [Range(typeof(decimal), "0.00", "99999999.99", ErrorMessage = "Discount must be zero or greater.")]
    public decimal DiscountAmount { get; set; }

    [StringLength(300)]
    public string Description { get; set; } = "Appointment consultation";

    public IReadOnlyList<LookupItem> EligibleAppointments { get; set; } = Array.Empty<LookupItem>();
    public decimal TaxRate { get; set; }
}

public sealed class BillRowViewModel
{
    public int BillId { get; init; }
    public string BillNumber { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string? AppointmentCode { get; init; }
    public DateTime BillDateLocal { get; init; }
    public decimal TotalAmount { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
}

public sealed class BillIndexViewModel
{
    public PagedResult<BillRowViewModel> Results { get; init; } = new();
    public string? Search { get; init; }
    public PaymentStatus? Status { get; init; }
}

public sealed class BillDetailsViewModel
{
    public Bill Bill { get; init; } = new();
    public DateTime BillDateLocal { get; init; }
    public DateTime? AppointmentDateTimeLocal { get; init; }
    public string TaxRateLabel => $"{Bill.TaxRate:P2}";
}
