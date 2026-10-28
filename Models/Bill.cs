namespace HospitalManagementSystem.Models;

public enum PaymentStatus
{
    Unpaid,
    Paid,
    Void
}

public sealed class Bill
{
    public int BillId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }
    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public DateTime BillDateUtc { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public string Description { get; set; } = string.Empty;
}
