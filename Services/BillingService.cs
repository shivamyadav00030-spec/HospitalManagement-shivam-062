using System.Data;
using System.Data.Common;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Services;

public sealed record BillOperationResult(bool Succeeded, string? Error, Bill? Bill = null);

public sealed class BillingService(
    HospitalDbContext db,
    IConfiguration configuration,
    IHospitalClock clock,
    ILogger<BillingService> logger)
{
    public decimal TaxRate => configuration.GetValue<decimal>("Billing:TaxRate");

    public async Task<BillOperationResult> GenerateAsync(int appointmentId, decimal discountAmount, string? description, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            // The appointment row lock makes the one-bill-per-appointment check safe for concurrent requests.
            var appointment = await db.Appointments.FromSqlInterpolated(
                $"SELECT * FROM Appointments WHERE AppointmentId = {appointmentId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (appointment is null)
                return await RollbackFailure(transaction, "The selected appointment was not found.", cancellationToken);

            await db.Entry(appointment).Reference(x => x.Doctor).LoadAsync(cancellationToken);
            if (appointment.Status != AppointmentStatus.Completed)
                return await RollbackFailure(transaction, "A bill can only be generated for a completed appointment.", cancellationToken);
            if (await db.Bills.AnyAsync(x => x.AppointmentId == appointmentId, cancellationToken))
                return await RollbackFailure(transaction, "A bill already exists for this appointment.", cancellationToken);

            Bill bill;
            try
            {
                bill = BillCalculator.CreateForAppointment(appointment, discountAmount, TaxRate, clock.UtcNow, description ?? "Appointment consultation");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return await RollbackFailure(transaction, ex.Message, cancellationToken);
            }
            db.Bills.Add(bill);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(true, null, bill);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException)
        {
            logger.LogWarning(ex, "A database error occurred while generating a bill for appointment {AppointmentId}.", appointmentId);
            return new(false, "The bill could not be generated because another request may have changed this appointment. Reload and try again.");
        }
    }

    public async Task<BillOperationResult> MarkPaidAsync(int billId, CancellationToken cancellationToken = default)
    {
        var bill = await db.Bills.SingleOrDefaultAsync(x => x.BillId == billId, cancellationToken);
        if (bill is null) return new(false, "Bill not found.");
        if (bill.PaymentStatus != PaymentStatus.Unpaid) return new(false, "Only unpaid bills can be marked paid.");
        bill.PaymentStatus = PaymentStatus.Paid;
        await db.SaveChangesAsync(cancellationToken);
        return new(true, null, bill);
    }

    private static async Task<BillOperationResult> RollbackFailure(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, string error, CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        return new(false, error);
    }
}
