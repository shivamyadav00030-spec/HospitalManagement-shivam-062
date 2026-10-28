using System.Data;
using System.Data.Common;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Services;

public sealed record AppointmentOperationResult(bool Succeeded, string? Error, Appointment? Appointment = null);

public sealed class AppointmentService(
    HospitalDbContext db,
    IHospitalClock clock,
    ILogger<AppointmentService> logger)
{
    public const int DurationMinutes = AppointmentRules.DurationMinutes;

    public async Task<AppointmentOperationResult> BookAsync(int patientId, int doctorId, DateTime localTime, string reason, CancellationToken cancellationToken = default)
    {
        if (!AppointmentRules.IsValidSlot(localTime))
            return new(false, $"Appointments use {DurationMinutes}-minute slots starting on the hour or half-hour.");

        DateTime startUtc;
        try { startUtc = clock.ToUtc(localTime); }
        catch (ArgumentException ex) { return new(false, ex.Message); }

        if (!AppointmentRules.IsFutureSlot(localTime, clock.UtcNow, clock))
            return new(false, "New appointments must be in the future.");

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            // Locking the doctor row serializes competing bookings for this doctor on MySQL/InnoDB.
            var doctor = await db.Doctors.FromSqlInterpolated(
                $"SELECT * FROM Doctors WHERE DoctorId = {doctorId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (doctor is null || !doctor.IsAvailable)
                return await RollbackFailure(transaction, "The selected doctor is missing or unavailable.", cancellationToken);

            var patientIsActive = await db.Patients.AnyAsync(x => x.PatientId == patientId && x.IsActive, cancellationToken);
            if (!patientIsActive)
                return await RollbackFailure(transaction, "Select an active registered patient.", cancellationToken);

            var slotTaken = await db.Appointments.AnyAsync(x => x.DoctorId == doctorId &&
                x.AppointmentDateTimeUtc == startUtc && x.Status != AppointmentStatus.Cancelled, cancellationToken);
            if (slotTaken)
                return await RollbackFailure(transaction, "That doctor already has an appointment in this time slot.", cancellationToken);

            var appointment = new Appointment
            {
                AppointmentCode = $"APT-{Guid.NewGuid():N}"[..32].ToUpperInvariant(),
                PatientId = patientId,
                DoctorId = doctorId,
                AppointmentDateTimeUtc = startUtc,
                ReasonForVisit = reason.Trim(),
                Status = AppointmentStatus.Scheduled,
                CreatedAtUtc = clock.UtcNow
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(true, null, appointment);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException)
        {
            logger.LogWarning(ex, "A database error occurred while booking an appointment for doctor {DoctorId}.", doctorId);
            return new(false, "The booking could not be confirmed because another request may have changed this doctor's schedule. Please try again.");
        }
    }

    public async Task<AppointmentOperationResult> RescheduleAsync(int appointmentId, DateTime localTime, CancellationToken cancellationToken = default)
    {
        if (!AppointmentRules.IsValidSlot(localTime))
            return new(false, $"Appointments use {DurationMinutes}-minute slots starting on the hour or half-hour.");

        DateTime startUtc;
        try { startUtc = clock.ToUtc(localTime); }
        catch (ArgumentException ex) { return new(false, ex.Message); }
        if (!AppointmentRules.IsFutureSlot(localTime, clock.UtcNow, clock))
            return new(false, "Appointments must remain in the future when rescheduled.");

        var current = await db.Appointments.SingleOrDefaultAsync(x => x.AppointmentId == appointmentId, cancellationToken);
        if (current is null) return new(false, "Appointment not found.");
        if (current.Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Confirmed))
            return new(false, "Only scheduled or confirmed appointments can be rescheduled.");

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var doctor = await db.Doctors.FromSqlInterpolated(
                $"SELECT * FROM Doctors WHERE DoctorId = {current.DoctorId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (doctor is null || !doctor.IsAvailable)
                return await RollbackFailure(transaction, "The selected doctor is missing or unavailable.", cancellationToken);

            var slotTaken = await db.Appointments.AnyAsync(x => x.AppointmentId != appointmentId &&
                x.DoctorId == current.DoctorId && x.AppointmentDateTimeUtc == startUtc &&
                x.Status != AppointmentStatus.Cancelled, cancellationToken);
            if (slotTaken) return await RollbackFailure(transaction, "That doctor already has an appointment in this time slot.", cancellationToken);

            current.AppointmentDateTimeUtc = startUtc;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(true, null, current);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException)
        {
            logger.LogWarning(ex, "A database error occurred while rescheduling appointment {AppointmentId}.", appointmentId);
            return new(false, "The appointment could not be rescheduled. Reload the record and try again.");
        }
    }

    public async Task<AppointmentOperationResult> ChangeStatusAsync(int appointmentId, AppointmentStatus nextStatus, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(nextStatus)) return new(false, "Invalid appointment status.");
        var appointment = await db.Appointments.SingleOrDefaultAsync(x => x.AppointmentId == appointmentId, cancellationToken);
        if (appointment is null) return new(false, "Appointment not found.");

        if (!AppointmentRules.CanTransition(appointment.Status, nextStatus))
            return new(false, $"Cannot change an appointment from {appointment.Status} to {nextStatus}.");
        if (nextStatus == AppointmentStatus.Confirmed && appointment.AppointmentDateTimeUtc <= clock.UtcNow)
            return new(false, "An appointment in the past cannot be confirmed.");
        if (nextStatus == AppointmentStatus.Completed && appointment.AppointmentDateTimeUtc > clock.UtcNow)
            return new(false, "A future appointment cannot be marked completed.");

        appointment.Status = nextStatus;
        await db.SaveChangesAsync(cancellationToken);
        return new(true, null, appointment);
    }

    private static async Task<AppointmentOperationResult> RollbackFailure(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, string error, CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        return new(false, error);
    }
}
