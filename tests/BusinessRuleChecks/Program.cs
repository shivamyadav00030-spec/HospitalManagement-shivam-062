using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;

var checks = new (string Name, Action Test)[]
{
    ("Bill totals apply discount before tax", () =>
    {
        var result = BillCalculator.Calculate(100m, 10m, 0.05m);
        Equal(4.50m, result.TaxAmount);
        Equal(94.50m, result.TotalAmount);
    }),
    ("Zero tax stays explicit and does not change the total", () =>
    {
        var result = BillCalculator.Calculate(149.99m, 0m, 0m);
        Equal(0m, result.TaxAmount);
        Equal(149.99m, result.TotalAmount);
    }),
    ("Monetary inputs are rounded consistently before storage", () =>
    {
        var result = BillCalculator.Calculate(100m, 1.005m, 0m);
        Equal(1.01m, result.DiscountAmount);
        Equal(98.99m, result.TotalAmount);
    }),
    ("Bill amounts reject negative fees", () => Throws<ArgumentOutOfRangeException>(() => BillCalculator.Calculate(-1m, 0m, 0m))),
    ("Bill amounts reject a discount above the subtotal", () => Throws<ArgumentOutOfRangeException>(() => BillCalculator.Calculate(50m, 50.01m, 0m))),
    ("Bill amounts reject tax rates above 100 percent", () => Throws<ArgumentOutOfRangeException>(() => BillCalculator.Calculate(50m, 0m, 1.01m))),
    ("Appointment slots start on the hour or half-hour", () =>
    {
        True(AppointmentRules.IsValidSlot(new DateTime(2026, 10, 24, 9, 0, 0)));
        True(AppointmentRules.IsValidSlot(new DateTime(2026, 10, 24, 9, 30, 0)));
        False(AppointmentRules.IsValidSlot(new DateTime(2026, 10, 24, 9, 15, 0)));
    }),
    ("Appointment slots must be in the future", () =>
    {
        var clock = new UtcClock();
        var now = new DateTime(2026, 10, 24, 3, 0, 0, DateTimeKind.Utc);
        True(AppointmentRules.IsFutureSlot(new DateTime(2026, 10, 24, 3, 30, 0), now, clock));
        False(AppointmentRules.IsFutureSlot(new DateTime(2026, 10, 24, 2, 30, 0), now, clock));
    }),
    ("Appointment status transitions block invalid reversals", () =>
    {
        True(AppointmentRules.CanTransition(AppointmentStatus.Scheduled, AppointmentStatus.Confirmed));
        True(AppointmentRules.CanTransition(AppointmentStatus.Confirmed, AppointmentStatus.Completed));
        False(AppointmentRules.CanTransition(AppointmentStatus.Completed, AppointmentStatus.Cancelled));
        False(AppointmentRules.CanTransition(AppointmentStatus.Cancelled, AppointmentStatus.Scheduled));
    }),
    ("A completed appointment bill stores server-calculated totals", () =>
    {
        var appointment = new Appointment
        {
            AppointmentId = 7, PatientId = 3, Status = AppointmentStatus.Completed,
            Doctor = new Doctor { ConsultationFee = 240m }
        };
        var bill = BillCalculator.CreateForAppointment(appointment, 40m, 0m,
            new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc), "Consultation");
        Equal(200m, bill.TotalAmount);
        Equal(0m, bill.TaxAmount);
        Equal(PaymentStatus.Unpaid, bill.PaymentStatus);
        True(bill.BillNumber.StartsWith("BILL-", StringComparison.Ordinal));
    })
};

var failures = new List<string>();
foreach (var (name, test) in checks)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

Console.WriteLine($"\n{checks.Length - failures.Count}/{checks.Length} business-rule checks passed.");
return failures.Count == 0 ? 0 : 1;

static void True(bool value)
{
    if (!value) throw new InvalidOperationException("Expected true.");
}

static void False(bool value) => True(!value);

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
}

static void Throws<TException>(Action action) where TException : Exception
{
    try { action(); }
    catch (TException) { return; }
    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

sealed class UtcClock : IHospitalClock
{
    public TimeZoneInfo TimeZone => TimeZoneInfo.Utc;
    public DateTime UtcNow => new(2026, 10, 24, 3, 0, 0, DateTimeKind.Utc);
    public DateTime ToLocal(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc);
    public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
}
