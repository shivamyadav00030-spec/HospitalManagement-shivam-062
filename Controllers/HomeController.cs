using System.Diagnostics;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;
using HospitalManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

public sealed class HomeController(HospitalDbContext db, IHospitalClock clock) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var nowLocal = clock.ToLocal(clock.UtcNow);
        var todayStartUtc = clock.ToUtc(DateTime.SpecifyKind(nowLocal.Date, DateTimeKind.Unspecified));
        var tomorrowStartUtc = clock.ToUtc(DateTime.SpecifyKind(nowLocal.Date.AddDays(1), DateTimeKind.Unspecified));
        var todayAppointments = db.Appointments.Where(x => x.AppointmentDateTimeUtc >= todayStartUtc && x.AppointmentDateTimeUtc < tomorrowStartUtc);

        var recent = await db.Appointments.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor!).ThenInclude(x => x.Department)
            .OrderByDescending(x => x.AppointmentDateTimeUtc)
            .Take(6)
            .Select(x => new AppointmentRowViewModel
            {
                AppointmentId = x.AppointmentId,
                AppointmentCode = x.AppointmentCode,
                PatientName = x.Patient!.FullName,
                PatientCode = x.Patient.PatientCode,
                DoctorName = x.Doctor!.FullName,
                DepartmentName = x.Doctor.Department!.DepartmentName,
                AppointmentDateTimeLocal = x.AppointmentDateTimeUtc,
                Status = x.Status
            }).ToListAsync(cancellationToken);

        var model = new DashboardViewModel
        {
            CurrentLocalTime = nowLocal,
            PatientCount = await db.Patients.CountAsync(cancellationToken),
            DoctorCount = await db.Doctors.CountAsync(cancellationToken),
            DepartmentCount = await db.Departments.CountAsync(cancellationToken),
            AppointmentCount = await db.Appointments.CountAsync(cancellationToken),
            TodayAppointmentCount = await todayAppointments.CountAsync(cancellationToken),
            PendingAppointmentCount = await db.Appointments.CountAsync(x => x.Status == AppointmentStatus.Scheduled || x.Status == AppointmentStatus.Confirmed, cancellationToken),
            BillCount = await db.Bills.CountAsync(cancellationToken),
            RecentAppointments = recent.Select(x => new AppointmentRowViewModel
            {
                AppointmentId = x.AppointmentId,
                AppointmentCode = x.AppointmentCode,
                PatientName = x.PatientName,
                PatientCode = x.PatientCode,
                DoctorName = x.DoctorName,
                DepartmentName = x.DepartmentName,
                AppointmentDateTimeLocal = clock.ToLocal(x.AppointmentDateTimeLocal),
                Status = x.Status
            }).ToList()
        };
        return View(model);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
