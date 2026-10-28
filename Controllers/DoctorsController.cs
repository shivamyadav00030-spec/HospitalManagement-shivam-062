using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;
using HospitalManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

public sealed class DoctorsController(HospitalDbContext db, IHospitalClock clock) : Controller
{
    private const int PageSize = 10;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var query = db.Doctors.AsNoTracking().Include(x => x.Department).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(x => x.FullName.Contains(value) || x.DoctorCode.Contains(value) || x.Specialization.Contains(value) || x.Department!.DepartmentName.Contains(value));
        }
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.FullName).Skip((page - 1) * PageSize).Take(PageSize)
            .Select(x => new DoctorListItemViewModel
            {
                DoctorId = x.DoctorId,
                DoctorCode = x.DoctorCode,
                FullName = x.FullName,
                Specialization = x.Specialization,
                DepartmentName = x.Department!.DepartmentName,
                ConsultationFee = x.ConsultationFee,
                IsAvailable = x.IsAvailable
            }).ToListAsync(cancellationToken);
        return View(new PagedResult<DoctorListItemViewModel>
        {
            Items = items, Page = page, PageSize = PageSize, TotalCount = count, Search = search
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) => View(await WithDepartments(new DoctorFormViewModel(), cancellationToken));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DoctorFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await db.Departments.AnyAsync(x => x.DepartmentId == model.DepartmentId && x.IsActive, cancellationToken))
            ModelState.AddModelError(nameof(model.DepartmentId), "Select an active department.");
        if (!ModelState.IsValid) return View(await WithDepartments(model, cancellationToken));

        var doctor = new Doctor
        {
            DoctorCode = $"DR-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            FullName = model.FullName.Trim(), DepartmentId = model.DepartmentId,
            Specialization = model.Specialization.Trim(), PhoneNumber = model.PhoneNumber.Trim(),
            Email = NullIfEmpty(model.Email), ConsultationFee = decimal.Round(model.ConsultationFee, 2), IsAvailable = true
        };
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Doctor {doctor.DoctorCode} was added.";
        return RedirectToAction(nameof(Details), new { id = doctor.DoctorId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var doctor = await db.Doctors.AsNoTracking().Include(x => x.Department).SingleOrDefaultAsync(x => x.DoctorId == id, cancellationToken);
        if (doctor is null) return NotFound();
        var count = await db.Appointments.CountAsync(x => x.DoctorId == id &&
            (x.Status == AppointmentStatus.Scheduled || x.Status == AppointmentStatus.Confirmed) && x.AppointmentDateTimeUtc >= clock.UtcNow, cancellationToken);
        return View(new DoctorDetailsViewModel { Doctor = doctor, UpcomingAppointmentCount = count });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var doctor = await db.Doctors.AsNoTracking().SingleOrDefaultAsync(x => x.DoctorId == id, cancellationToken);
        if (doctor is null) return NotFound();
        var model = new DoctorFormViewModel
        {
            DoctorId = doctor.DoctorId, FullName = doctor.FullName, DepartmentId = doctor.DepartmentId,
            Specialization = doctor.Specialization, PhoneNumber = doctor.PhoneNumber, Email = doctor.Email,
            ConsultationFee = doctor.ConsultationFee
        };
        return View(await WithDepartments(model, cancellationToken));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DoctorFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.DoctorId) return BadRequest();
        if (!await db.Departments.AnyAsync(x => x.DepartmentId == model.DepartmentId && x.IsActive, cancellationToken))
            ModelState.AddModelError(nameof(model.DepartmentId), "Select an active department.");
        if (!ModelState.IsValid) return View(await WithDepartments(model, cancellationToken));

        var doctor = await db.Doctors.SingleOrDefaultAsync(x => x.DoctorId == id, cancellationToken);
        if (doctor is null) return NotFound();
        doctor.FullName = model.FullName.Trim(); doctor.DepartmentId = model.DepartmentId;
        doctor.Specialization = model.Specialization.Trim(); doctor.PhoneNumber = model.PhoneNumber.Trim();
        doctor.Email = NullIfEmpty(model.Email); doctor.ConsultationFee = decimal.Round(model.ConsultationFee, 2);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Doctor information was updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAvailability(int id, bool isAvailable, CancellationToken cancellationToken)
    {
        var doctor = await db.Doctors.SingleOrDefaultAsync(x => x.DoctorId == id, cancellationToken);
        if (doctor is null) return NotFound();
        doctor.IsAvailable = isAvailable;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = isAvailable ? "Doctor is available for booking." : "Doctor was deactivated; historical records were retained.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<DoctorFormViewModel> WithDepartments(DoctorFormViewModel model, CancellationToken cancellationToken)
    {
        model.Departments = await db.Departments.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DepartmentName)
            .Select(x => new LookupItem { Id = x.DepartmentId, Label = x.DepartmentName }).ToListAsync(cancellationToken);
        return model;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
