using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;
using HospitalManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

public sealed class PatientsController(HospitalDbContext db, IHospitalClock clock) : Controller
{
    private const int PageSize = 10;
    private static readonly string[] Genders = ["Female", "Male", "Other", "Prefer not to say"];

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        var query = db.Patients.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var code = search.Trim();
            query = query.Where(x => x.PatientCode.Contains(code));
        }

        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.PatientCode)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(x => new PatientListItemViewModel
            {
                PatientId = x.PatientId,
                PatientCode = x.PatientCode,
                FullName = x.FullName,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                IsActive = x.IsActive
            }).ToListAsync(cancellationToken);

        return View(new PagedResult<PatientListItemViewModel>
        {
            Items = items, Page = page, PageSize = PageSize, TotalCount = count, Search = search
        });
    }

    [HttpGet]
    public IActionResult Create() => View(new PatientFormViewModel { GenderOptions = Genders });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PatientFormViewModel model, CancellationToken cancellationToken)
    {
        ValidatePatient(model);
        model.GenderOptions = Genders;
        if (!ModelState.IsValid) return View(model);

        var patient = new Patient
        {
            PatientCode = $"PT-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            FullName = model.FullName.Trim(),
            DateOfBirth = DateOnly.FromDateTime(model.DateOfBirth),
            Gender = model.Gender,
            PhoneNumber = model.PhoneNumber.Trim(),
            Email = NullIfEmpty(model.Email),
            Address = model.Address.Trim(),
            RegistrationDateUtc = clock.UtcNow,
            IsActive = true
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Patient {patient.PatientCode} was registered.";
        return RedirectToAction(nameof(Details), new { id = patient.PatientId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var patient = await db.Patients.AsNoTracking().SingleOrDefaultAsync(x => x.PatientId == id, cancellationToken);
        if (patient is null) return NotFound();
        var recent = await db.Appointments.AsNoTracking().Where(x => x.PatientId == id)
            .Include(x => x.Doctor!).ThenInclude(x => x.Department)
            .OrderByDescending(x => x.AppointmentDateTimeUtc).Take(5)
            .Select(x => new AppointmentRowViewModel
            {
                AppointmentId = x.AppointmentId,
                AppointmentCode = x.AppointmentCode,
                PatientName = patient.FullName,
                PatientCode = patient.PatientCode,
                DoctorName = x.Doctor!.FullName,
                DepartmentName = x.Doctor.Department!.DepartmentName,
                AppointmentDateTimeLocal = x.AppointmentDateTimeUtc,
                Status = x.Status
            }).ToListAsync(cancellationToken);
        return View(new PatientDetailsViewModel
        {
            Patient = patient,
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
            }).ToList(),
            BillCount = await db.Bills.CountAsync(x => x.PatientId == id, cancellationToken)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var patient = await db.Patients.AsNoTracking().SingleOrDefaultAsync(x => x.PatientId == id, cancellationToken);
        if (patient is null) return NotFound();
        var model = ToForm(patient);
        model.GenderOptions = Genders;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PatientFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.PatientId) return BadRequest();
        ValidatePatient(model);
        model.GenderOptions = Genders;
        if (!ModelState.IsValid) return View(model);

        var patient = await db.Patients.SingleOrDefaultAsync(x => x.PatientId == id, cancellationToken);
        if (patient is null) return NotFound();
        patient.FullName = model.FullName.Trim();
        patient.DateOfBirth = DateOnly.FromDateTime(model.DateOfBirth);
        patient.Gender = model.Gender;
        patient.PhoneNumber = model.PhoneNumber.Trim();
        patient.Email = NullIfEmpty(model.Email);
        patient.Address = model.Address.Trim();
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Patient details were updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var patient = await db.Patients.SingleOrDefaultAsync(x => x.PatientId == id, cancellationToken);
        if (patient is null) return NotFound();
        patient.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Patient was deactivated. Historical appointments and bills were retained.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private void ValidatePatient(PatientFormViewModel model)
    {
        var today = DateOnly.FromDateTime(clock.ToLocal(clock.UtcNow));
        if (model.DateOfBirth == default || DateOnly.FromDateTime(model.DateOfBirth) > today)
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth must be a valid date in the past.");
        if (!Genders.Contains(model.Gender, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.Gender), "Select a listed gender option.");
    }

    private static PatientFormViewModel ToForm(Patient p) => new()
    {
        PatientId = p.PatientId,
        FullName = p.FullName,
        DateOfBirth = p.DateOfBirth.ToDateTime(TimeOnly.MinValue),
        Gender = p.Gender,
        PhoneNumber = p.PhoneNumber,
        Email = p.Email,
        Address = p.Address
    };

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
