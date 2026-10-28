using HospitalManagementSystem.Models;

namespace HospitalManagementSystem.Services;

public readonly record struct BillAmounts(decimal Subtotal, decimal DiscountAmount, decimal TaxRate, decimal TaxAmount, decimal TotalAmount);

public static class BillCalculator
{
    public static BillAmounts Calculate(decimal subtotal, decimal discountAmount, decimal taxRate)
    {
        if (subtotal < 0) throw new ArgumentOutOfRangeException(nameof(subtotal), "Subtotal cannot be negative.");
        if (discountAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(discountAmount), "Discount cannot be negative.");
        if (taxRate is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate must be between 0 and 1.");

        subtotal = decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero);
        discountAmount = decimal.Round(discountAmount, 2, MidpointRounding.AwayFromZero);
        taxRate = decimal.Round(taxRate, 4, MidpointRounding.AwayFromZero);
        if (discountAmount > subtotal)
            throw new ArgumentOutOfRangeException(nameof(discountAmount), "Discount must not exceed the subtotal.");

        var taxableAmount = subtotal - discountAmount;
        var taxAmount = decimal.Round(taxableAmount * taxRate, 2, MidpointRounding.AwayFromZero);
        var total = decimal.Round(taxableAmount + taxAmount, 2, MidpointRounding.AwayFromZero);
        return new BillAmounts(subtotal, discountAmount, taxRate, taxAmount, total);
    }

    public static Bill CreateForAppointment(Appointment appointment, decimal discountAmount, decimal taxRate, DateTime nowUtc, string description)
    {
        if (appointment.Status != AppointmentStatus.Completed)
            throw new InvalidOperationException("A bill can only be generated for a completed appointment.");

        var amounts = Calculate(appointment.Doctor?.ConsultationFee ?? 0m, discountAmount, taxRate);
        return new Bill
        {
            BillNumber = $"BILL-{Guid.NewGuid():N}"[..32].ToUpperInvariant(),
            PatientId = appointment.PatientId,
            AppointmentId = appointment.AppointmentId,
            BillDateUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
            Subtotal = amounts.Subtotal,
            DiscountAmount = amounts.DiscountAmount,
            TaxRate = amounts.TaxRate,
            TaxAmount = amounts.TaxAmount,
            TotalAmount = amounts.TotalAmount,
            PaymentStatus = PaymentStatus.Unpaid,
            Description = string.IsNullOrWhiteSpace(description) ? "Appointment consultation" : description.Trim()
        };
    }
}
