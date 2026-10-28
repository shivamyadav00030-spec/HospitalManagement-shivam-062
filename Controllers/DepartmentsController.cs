using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

public sealed class DepartmentsController(HospitalDbContext db) : Controller
{
    private const int PageSize = 10;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        var query = db.Departments.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(x => x.DepartmentName.Contains(value));
        }
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.DepartmentName).Skip((page - 1) * PageSize).Take(PageSize)
            .Select(x => new DepartmentListItemViewModel
            {
                DepartmentId = x.DepartmentId, DepartmentName = x.DepartmentName, Description = x.Description,
                DoctorCount = x.Doctors.Count, IsActive = x.IsActive
            }).ToListAsync(cancellationToken);
        return View(new PagedResult<DepartmentListItemViewModel>
        {
            Items = items, Page = page, PageSize = PageSize, TotalCount = count, Search = search
        });
    }

    [HttpGet]
    public IActionResult Create() => View(new DepartmentFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentFormViewModel model, CancellationToken cancellationToken)
    {
        var name = model.DepartmentName.Trim();
        if (await db.Departments.AnyAsync(x => x.DepartmentName == name, cancellationToken))
            ModelState.AddModelError(nameof(model.DepartmentName), "A department with that name already exists.");
        if (!ModelState.IsValid) return View(model);
        var department = new Department { DepartmentName = name, Description = NullIfEmpty(model.Description), IsActive = true };
        db.Departments.Add(department);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.DepartmentName), "The department name may already be in use. Refresh the page and try again.");
            return View(model);
        }
        TempData["Success"] = "Department was added.";
        return RedirectToAction(nameof(Details), new { id = department.DepartmentId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var department = await db.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.DepartmentId == id, cancellationToken);
        if (department is null) return NotFound();
        var doctors = await db.Doctors.AsNoTracking().Where(x => x.DepartmentId == id).OrderBy(x => x.FullName)
            .Select(x => new DoctorListItemViewModel
            {
                DoctorId = x.DoctorId, DoctorCode = x.DoctorCode, FullName = x.FullName,
                Specialization = x.Specialization, DepartmentName = department.DepartmentName,
                ConsultationFee = x.ConsultationFee, IsAvailable = x.IsAvailable
            }).ToListAsync(cancellationToken);
        return View(new DepartmentDetailsViewModel { Department = department, Doctors = doctors });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var department = await db.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.DepartmentId == id, cancellationToken);
        if (department is null) return NotFound();
        return View(new DepartmentFormViewModel { DepartmentId = id, DepartmentName = department.DepartmentName, Description = department.Description });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DepartmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.DepartmentId) return BadRequest();
        var name = model.DepartmentName.Trim();
        if (await db.Departments.AnyAsync(x => x.DepartmentId != id && x.DepartmentName == name, cancellationToken))
            ModelState.AddModelError(nameof(model.DepartmentName), "A department with that name already exists.");
        if (!ModelState.IsValid) return View(model);
        var department = await db.Departments.SingleOrDefaultAsync(x => x.DepartmentId == id, cancellationToken);
        if (department is null) return NotFound();
        department.DepartmentName = name;
        department.Description = NullIfEmpty(model.Description);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.DepartmentName), "The department name may already be in use. Refresh the page and try again.");
            return View(model);
        }
        TempData["Success"] = "Department details were updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var department = await db.Departments.SingleOrDefaultAsync(x => x.DepartmentId == id, cancellationToken);
        if (department is null) return NotFound();
        if (await db.Doctors.AnyAsync(x => x.DepartmentId == id, cancellationToken))
        {
            TempData["Error"] = "This department has doctors assigned to it. Move or deactivate those records before deletion.";
            return RedirectToAction(nameof(Details), new { id });
        }
        db.Departments.Remove(department);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Department was deleted.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "This department is still referenced by historical records and cannot be deleted.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
