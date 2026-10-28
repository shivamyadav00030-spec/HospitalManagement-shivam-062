using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;
using HospitalManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

public sealed class BillsController(HospitalDbContext db, BillingService billing, IHospitalClock clock) : Controller
{
    private const int PageSize = 10;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, PaymentStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        var query = db.Bills.AsNoTracking().Include(x => x.Patient).Include(x => x.Appointment).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(x => x.BillNumber.Contains(value) || x.Patient!.PatientCode.Contains(value));
        }
        if (status.HasValue) query = query.Where(x => x.PaymentStatus == status.Value);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.BillDateUtc).Skip((page - 1) * PageSize).Take(PageSize)
            .Select(x => new BillRowViewModel
            {
                BillId = x.BillId, BillNumber = x.BillNumber, PatientName = x.Patient!.FullName,
                AppointmentCode = x.Appointment != null ? x.Appointment.AppointmentCode : null,
                BillDateLocal = x.BillDateUtc, TotalAmount = x.TotalAmount, PaymentStatus = x.PaymentStatus
            }).ToListAsync(cancellationToken);
        return View(new BillIndexViewModel
        {
            Results = new PagedResult<BillRowViewModel>
            {
                Items = items.Select(x => new BillRowViewModel
                {
                    BillId = x.BillId, BillNumber = x.BillNumber, PatientName = x.PatientName,
                    AppointmentCode = x.AppointmentCode, BillDateLocal = clock.ToLocal(x.BillDateLocal),
                    TotalAmount = x.TotalAmount, PaymentStatus = x.PaymentStatus
                }).ToList(),
                Page = page, PageSize = PageSize, TotalCount = count, Search = search
            },
            Search = search,
            Status = status
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) => View(await WithAppointments(new BillCreateViewModel(), cancellationToken));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BillCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(await WithAppointments(model, cancellationToken));
        var result = await billing.GenerateAsync(model.AppointmentId, model.DiscountAmount, model.Description, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Bill could not be generated.");
            return View(await WithAppointments(model, cancellationToken));
        }
        TempData["Success"] = $"Bill {result.Bill!.BillNumber} was generated.";
        return RedirectToAction(nameof(Details), new { id = result.Bill.BillId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var bill = await LoadBill(id, cancellationToken);
        if (bill is null) return NotFound();
        return View(new BillDetailsViewModel
        {
            Bill = bill,
            BillDateLocal = clock.ToLocal(bill.BillDateUtc),
            AppointmentDateTimeLocal = bill.Appointment is null ? null : clock.ToLocal(bill.Appointment.AppointmentDateTimeUtc)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Invoice(int id, CancellationToken cancellationToken)
    {
        var bill = await LoadBill(id, cancellationToken);
        if (bill is null) return NotFound();
        return View(new BillDetailsViewModel
        {
            Bill = bill,
            BillDateLocal = clock.ToLocal(bill.BillDateUtc),
            AppointmentDateTimeLocal = bill.Appointment is null ? null : clock.ToLocal(bill.Appointment.AppointmentDateTimeUtc)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(int id, CancellationToken cancellationToken)
    {
        var result = await billing.MarkPaidAsync(id, cancellationToken);
        if (!result.Succeeded) TempData["Error"] = result.Error;
        else TempData["Success"] = "Bill marked as paid for demonstration purposes.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<Bill?> LoadBill(int id, CancellationToken cancellationToken) =>
        await db.Bills.AsNoTracking().Include(x => x.Patient)
            .Include(x => x.Appointment!).ThenInclude(x => x.Doctor!).ThenInclude(x => x.Department)
            .SingleOrDefaultAsync(x => x.BillId == id, cancellationToken);

    private async Task<BillCreateViewModel> WithAppointments(BillCreateViewModel model, CancellationToken cancellationToken)
    {
        model.TaxRate = billing.TaxRate;
        model.EligibleAppointments = await db.Appointments.AsNoTracking()
            .Where(x => x.Status == AppointmentStatus.Completed && !db.Bills.Any(b => b.AppointmentId == x.AppointmentId))
            .Include(x => x.Patient).Include(x => x.Doctor)
            .OrderByDescending(x => x.AppointmentDateTimeUtc)
            .Select(x => new LookupItem
            {
                Id = x.AppointmentId,
                Label = $"{x.AppointmentCode} — {x.Patient!.PatientCode}, {x.Patient.FullName} — {x.Doctor!.FullName}"
            }).ToListAsync(cancellationToken);
        return model;
    }
}
