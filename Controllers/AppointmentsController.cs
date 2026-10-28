using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;
using HospitalManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

public sealed class AppointmentsController(
    HospitalDbContext db,
    AppointmentService appointmentService,
    IHospitalClock clock) : Controller
{
    private const int PageSize = 12;
    private const string FilterSessionKey = "appointment.filters.v1";

    private sealed record SavedFilter(string? PatientCode, int? DoctorId, DateOnly? Date, AppointmentStatus? Status);

    [HttpGet]
    public async Task<IActionResult> Index(AppointmentFilterViewModel filter, bool clear = false, CancellationToken cancellationToken = default)
    {
        if (clear)
        {
            HttpContext.Session.Remove(FilterSessionKey);
            filter = new AppointmentFilterViewModel();
        }
        else
        {
            var hasFilterInput = Request.Query.ContainsKey(nameof(filter.PatientSearch)) ||
                Request.Query.ContainsKey(nameof(filter.DoctorId)) || Request.Query.ContainsKey(nameof(filter.Date)) ||
                Request.Query.ContainsKey(nameof(filter.Status));
            if (!hasFilterInput)
            {
                var saved = HttpContext.Session.GetJson<SavedFilter>(FilterSessionKey);
                if (saved is not null)
                {
                    filter.PatientSearch = saved.PatientCode;
                    filter.DoctorId = saved.DoctorId;
                    filter.Date = saved.Date;
                    filter.Status = saved.Status;
                }
            }
            else
            {
                HttpContext.Session.SetJson(FilterSessionKey, new SavedFilter(
                    string.IsNullOrWhiteSpace(filter.PatientSearch) ? null : filter.PatientSearch.Trim(),
                    filter.DoctorId, filter.Date, filter.Status));
            }
        }

        filter.Page = Math.Max(filter.Page, 1);
        var query = db.Appointments.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor!).ThenInclude(x => x.Department)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.PatientSearch))
        {
            var code = filter.PatientSearch.Trim();
            query = query.Where(x => x.Patient!.PatientCode.Contains(code));
        }
        if (filter.DoctorId.HasValue) query = query.Where(x => x.DoctorId == filter.DoctorId.Value);
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status.Value);
        if (filter.Date.HasValue)
        {
            var day = filter.Date.Value.ToDateTime(TimeOnly.MinValue);
            var startUtc = clock.ToUtc(DateTime.SpecifyKind(day, DateTimeKind.Unspecified));
            var endUtc = clock.ToUtc(DateTime.SpecifyKind(day.AddDays(1), DateTimeKind.Unspecified));
            query = query.Where(x => x.AppointmentDateTimeUtc >= startUtc && x.AppointmentDateTimeUtc < endUtc);
        }

        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.AppointmentDateTimeUtc)
            .Skip((filter.Page - 1) * PageSize).Take(PageSize)
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
        var doctors = await db.Doctors.AsNoTracking().Include(x => x.Department).OrderBy(x => x.FullName)
            .Select(x => new LookupItem { Id = x.DoctorId, Label = $"{x.FullName} — {x.Department!.DepartmentName}" }).ToListAsync(cancellationToken);

        return View(new AppointmentIndexViewModel
        {
            Results = new PagedResult<AppointmentRowViewModel>
            {
                Items = items.Select(ToLocal).ToList(), Page = filter.Page, PageSize = PageSize,
                TotalCount = count, Search = filter.PatientSearch
            },
            Filter = filter,
            Doctors = doctors,
            TimeZoneName = clock.TimeZone.Id
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? patientId, int? doctorId, CancellationToken cancellationToken)
    {
        var model = new AppointmentFormViewModel
        {
            PatientId = patientId ?? 0,
            DoctorId = doctorId ?? 0,
            AppointmentDateTimeLocal = clock.ToLocal(clock.UtcNow).Date.AddDays(1).AddHours(9),
            TimeZoneName = clock.TimeZone.Id
        };
        return View(await WithLookups(model, cancellationToken));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppointmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(await WithLookups(model, cancellationToken));
        var result = await appointmentService.BookAsync(model.PatientId, model.DoctorId, model.AppointmentDateTimeLocal,
            model.ReasonForVisit, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Appointment could not be booked.");
            return View(await WithLookups(model, cancellationToken));
        }
        TempData["Success"] = $"Appointment {result.Appointment!.AppointmentCode} was booked.";
        return RedirectToAction(nameof(Details), new { id = result.Appointment.AppointmentId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor!).ThenInclude(x => x.Department)
            .SingleOrDefaultAsync(x => x.AppointmentId == id, cancellationToken);
        if (appointment is null) return NotFound();
        return View(new AppointmentDetailsViewModel
        {
            Appointment = appointment,
            AppointmentDateTimeLocal = clock.ToLocal(appointment.AppointmentDateTimeUtc),
            TimeZoneName = clock.TimeZone.Id,
            CanMarkCompleted = appointment.AppointmentDateTimeUtc <= clock.UtcNow
        });
    }

    [HttpGet]
    public async Task<IActionResult> Reschedule(int id, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments.AsNoTracking().SingleOrDefaultAsync(x => x.AppointmentId == id, cancellationToken);
        if (appointment is null) return NotFound();
        if (appointment.Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Confirmed))
        {
            TempData["Error"] = "Only scheduled or confirmed appointments can be rescheduled.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(new RescheduleViewModel
        {
            AppointmentId = id,
            AppointmentCode = appointment.AppointmentCode,
            AppointmentDateTimeLocal = clock.ToLocal(appointment.AppointmentDateTimeUtc),
            TimeZoneName = clock.TimeZone.Id
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, RescheduleViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.AppointmentId) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        var result = await appointmentService.RescheduleAsync(id, model.AppointmentDateTimeLocal, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Appointment could not be rescheduled.");
            return View(model);
        }
        TempData["Success"] = "Appointment was rescheduled.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, AppointmentStatus nextStatus, CancellationToken cancellationToken)
    {
        var result = await appointmentService.ChangeStatusAsync(id, nextStatus, cancellationToken);
        if (!result.Succeeded) TempData["Error"] = result.Error;
        else TempData["Success"] = $"Appointment status changed to {nextStatus}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<AppointmentFormViewModel> WithLookups(AppointmentFormViewModel model, CancellationToken cancellationToken)
    {
        model.TimeZoneName = clock.TimeZone.Id;
        model.Patients = await db.Patients.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.FullName)
            .Select(x => new LookupItem { Id = x.PatientId, Label = $"{x.PatientCode} — {x.FullName}" }).ToListAsync(cancellationToken);
        model.Doctors = await db.Doctors.AsNoTracking().Where(x => x.IsAvailable).Include(x => x.Department).OrderBy(x => x.FullName)
            .Select(x => new LookupItem { Id = x.DoctorId, Label = $"{x.FullName} — {x.Specialization}, {x.Department!.DepartmentName}" }).ToListAsync(cancellationToken);
        return model;
    }

    private AppointmentRowViewModel ToLocal(AppointmentRowViewModel item) => new()
    {
        AppointmentId = item.AppointmentId, AppointmentCode = item.AppointmentCode,
        PatientName = item.PatientName, PatientCode = item.PatientCode,
        DoctorName = item.DoctorName, DepartmentName = item.DepartmentName,
        AppointmentDateTimeLocal = clock.ToLocal(item.AppointmentDateTimeLocal), Status = item.Status
    };
}
