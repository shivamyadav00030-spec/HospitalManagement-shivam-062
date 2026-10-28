# Hospital Management System

**A Database-Driven Hospital Administration and Patient Management Web Application**

## Project overview

This university mini-project demonstrates an ASP.NET Core MVC application for administrative hospital records. Staff can manage fictional patient, doctor, department, appointment, and billing records in a persistent MySQL database.

The project is for education and demonstration. It is not a clinically validated system. Do not enter real patient data. It does not provide diagnosis, treatment recommendations, emergency triage, or payment processing.

## Problem statement

Small hospital administration workflows often keep patient registrations, staff directories, appointments, and billing records in separate files. This project demonstrates a centralized database-backed interface and the relationships, validation, and state management needed to maintain those records consistently.

## Objectives

- Demonstrate MVC controllers, strongly typed Razor views, and dependency injection.
- Persist records in MySQL through Entity Framework Core.
- Implement CRUD workflows, search, pagination, and server-side validation.
- Keep appointment times consistent, reject duplicate time slots, and preserve historical records.
- Demonstrate temporary ASP.NET Core Session state without storing permanent or sensitive data there.
- Provide a responsive administration interface and a print-friendly educational invoice.

## Technology stack

| Technology | Version / role |
|---|---|
| .NET SDK / ASP.NET Core MVC | .NET 10 |
| C# and Razor | MVC application and strongly typed views |
| Entity Framework Core | 10.0.12 |
| MySQL EF Core provider | Oracle MySql.EntityFrameworkCore 10.0.12 |
| MySQL Server | 8.0 or later |
| Bootstrap | Local bundled CSS and JavaScript |
| JavaScript | Sidebar toggle and confirmation prompts |
| Git | Source control |

The Oracle provider package metadata lists EF Core 10 support, and Oracle's Connector/NET guide specifies MySQL Server 8.0 or later. See [provider package compatibility](https://www.nuget.org/packages/MySql.EntityFrameworkCore) and [Connector/NET EF Core support](https://dev.mysql.com/doc/connector-net/en/connector-net-entityframework-core.html).

## Environment check recorded for this workspace

- Workspace was empty apart from the outputs and work directories.
- Installed .NET SDK: 10.0.402; installed ASP.NET Core runtime: 10.0.13.
- Installed Git: 2.53.0.windows.3.
- No MySQL client or MySQL/MariaDB Windows service was found.
- dotnet-ef was not installed. A local tool manifest pins it to 10.0.12. Tool installation could not reach NuGet in this session, so migration application and database connectivity have not been verified here.
- The project itself restores and builds. A project-local NuGet.Config avoids the inaccessible user-level NuGet configuration file.

### Missing prerequisites

Install [MySQL Community Server 8.0 or later](https://dev.mysql.com/downloads/mysql/) and ensure its Windows service is running. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and [Git for Windows](https://git-scm.com/download/win) if they are not available on the machine where the project will be demonstrated. The local EF tool is restored from the project manifest with dotnet tool restore --configfile .\NuGet.Config when NuGet is reachable.

## Architecture

    Browser
      │ HTTP requests / Razor forms
      ▼
    ASP.NET Core MVC Controllers ── ViewModels and server-side validation
      │                                  │
      │ EF Core queries / transactions   └── Razor views + Bootstrap
      ▼
    HospitalDbContext
      │
      ▼
    MySQL HospitalManagementDB

    AppointmentsController ── Session (temporary non-sensitive filters only)

Controllers handle HTTP and map input ViewModels to entities. Business rules for appointment booking and invoice calculations live in services. HospitalDbContext defines the relationships, indexes, seed departments, and delete behavior. ASP.NET Core dependency injection supplies the context and services.

## Project structure

    HospitalManagementSystem/
    ├── Controllers/             MVC actions for each module
    ├── Data/
    │   └── HospitalDbContext.cs Entity sets, relationships, indexes, seed data
    ├── Migrations/              Initial MySQL migration and model snapshot
    ├── Models/                  Patient, Doctor, Department, Appointment, Bill
    ├── Services/                Appointment, billing, clock, and Session helpers
    ├── ViewModels/              Form, list, dashboard, and invoice models
    ├── Views/                   Strongly typed Razor pages and shared layout
    ├── wwwroot/                 Local Bootstrap, CSS, JavaScript
    ├── tests/BusinessRuleChecks/ Executable business-rule checks
    ├── .config/dotnet-tools.json Pinned local dotnet-ef tool
    ├── appsettings.json         Placeholder connection string and app settings
    ├── NuGet.Config             Project-local NuGet source
    ├── Program.cs               Dependency injection and middleware pipeline
    └── README.md

## Database schema

The database is named HospitalManagementDB. The migration creates:

- Departments: unique department name; seeded with Cardiology, General Medicine, Orthopedics, and Pediatrics.
- Doctors: unique doctor code, department foreign key, and consultation fee stored as decimal(10,2).
- Patients: unique patient code, demographic/contact fields, UTC registration time, and active flag.
- Appointments: unique appointment code, patient and doctor foreign keys, UTC appointment time, reason, and status.
- Bills: unique bill number, patient foreign key, optional unique appointment foreign key, itemized monetary amounts, configured tax rate, and payment status.

Relationships are Department 1-to-many Doctor, Patient 1-to-many Appointment, Doctor 1-to-many Appointment, Patient 1-to-many Bill, and Appointment 0-or-1-to-1 Bill. Foreign keys use restrictive delete behavior so historical records are protected. Patients and doctors are deactivated instead of deleting records that may be referenced by history. A department with doctors cannot be deleted.

## MySQL setup

1. Install and start MySQL Community Server 8.0 or later. On Windows, check for the service with Get-Service *mysql*; start the listed service with Start-Service <service-name> if needed.
2. Connect through MySQL Workbench or the MySQL command-line client as an administrator and create a database and local application account. Replace the sample password with a private value:

        CREATE DATABASE HospitalManagementDB
          CHARACTER SET utf8mb4;

        CREATE USER 'hospital_app'@'localhost'
          IDENTIFIED BY 'choose-a-local-password';

        GRANT ALL PRIVILEGES ON HospitalManagementDB.*
          TO 'hospital_app'@'localhost';

3. From the project directory, set a local user secret. This keeps the actual password outside source control:

        dotnet user-secrets init
        dotnet user-secrets set "ConnectionStrings:HospitalDatabase" "Server=localhost;Port=3306;Database=HospitalManagementDB;User Id=hospital_app;Password=YOUR_LOCAL_PASSWORD;"

appsettings.json contains only a placeholder (CHANGE_ME). Do not replace it with a real password before committing. The connection string is read from ASP.NET Core configuration; user secrets override the placeholder during local development.

## Run in Visual Studio Code

Open this folder in VS Code:

        cd outputs/HospitalManagementSystem
        code .

Then use the VS Code integrated terminal:

        dotnet restore --configfile .\NuGet.Config
        dotnet tool restore --configfile .\NuGet.Config
        dotnet user-secrets init
        dotnet user-secrets set "ConnectionStrings:HospitalDatabase" "Server=localhost;Port=3306;Database=HospitalManagementDB;User Id=hospital_app;Password=YOUR_LOCAL_PASSWORD;"
        dotnet ef database update
        dotnet build --no-restore
        dotnet run --no-restore

Open the HTTPS or HTTP local URL printed by dotnet run. The seeded departments are applied by the migration. This repository already includes its initial migration; do not add a second initial migration.

### EF Core tooling and migrations

The project tool manifest pins dotnet-ef to 10.0.12. To install or restore that local tool on a machine with NuGet access:

        dotnet tool restore --configfile .\NuGet.Config
        dotnet ef --version

For a fresh project that does not yet have a local tool manifest, the install commands are:

        dotnet new tool-manifest
        dotnet tool install dotnet-ef --version 10.0.12

For a new project before its first migration, use:

        dotnet ef migrations add InitialCreate
        dotnet ef database update

For this repository, the initial migration is already included. Apply it with dotnet ef database update. After changing entities or OnModelCreating, create and apply a new migration:

        dotnet ef migrations add AddYourChange
        dotnet ef database update

The initial migration and snapshot are included as source and compile, but they were not generated or checked by dotnet-ef: the tool could not be restored from NuGet in this workspace. Apply and verify them with the EF CLI against MySQL before presenting database behavior as verified; regenerate the migration with dotnet-ef if the tool reports a model mismatch.

## Modules

### Dashboard

Counts for patients, doctors, departments, appointments, today's appointments, pending appointments, bills, and recent appointments are queried from MySQL. Empty result sets show an empty state; counts are never hardcoded.

### Patients

Create, search by patient code, page through, view, edit, and deactivate patient records. Patient codes are generated by the server and uniquely indexed. Email, phone, required fields, string length, and date-of-birth checks run server-side. Deactivation preserves appointments and bills.

### Doctors

Create, search, page through, view, edit, deactivate, or reactivate doctors. A doctor must reference an active department when created or edited. Consultation fees use decimal precision and cannot be negative.

### Departments

Create, search, page through, view, and edit departments. Duplicate names are checked in the controller and protected by a unique index. Deletion is blocked when a doctor is assigned or historical relationships prevent deletion.

### Appointments

Book, filter, view, reschedule, confirm, complete, or cancel appointments. Only active patients and available doctors can be booked. The duration is fixed at 30 minutes; allowed start times are on the hour or half-hour. New bookings must be in the future. Booking checks run inside a serializable transaction and lock the doctor row before checking the slot. Cancelled slots may be reused. MySQL/InnoDB transactions serialize application bookings for the same doctor; direct database writes that bypass the application do not follow this rule.

Appointment input/display time zone is configured by Hospital:TimeZoneId (default Asia/Kolkata). Appointment and creation times are stored as UTC datetime(6) values. Session remembers only appointment filter values (patient code, doctor ID, date, and status) for 30 minutes; it does not hold patient records or confirmed bookings.

### Billing

Generate one bill for a completed appointment that has no existing bill. The subtotal comes from the doctor's stored consultation fee; discount, tax, and total are calculated on the server. Tax rate is explicitly configured as Billing:TaxRate, defaulting to 0.0 because no legally applicable rate is assumed. Negative fees/discounts/totals and discounts above the subtotal are rejected. Bills can be searched, listed, viewed, printed, and marked paid for demonstration. There is no payment processing or card storage. The module is an educational billing example, not a tax document.

## Form handling and data integrity

MVC model binding populates dedicated input ViewModels. Data annotations cover required fields, lengths, email, phone, and monetary ranges. Controllers check model state and database-backed foreign keys before mapping values to entities. Every state-changing action is POST-only and validates an antiforgery token. User input is queried through EF Core parameters. Unique indexes provide a second line of defense for patient/doctor/appointment/bill codes and department names.

## Session and database persistence

Session uses AddDistributedMemoryCache, AddSession, and UseSession after routing and before MVC endpoint execution. Its idle timeout is 30 minutes. The Appointments list restores and updates only the temporary filter values. Use Clear filters to remove that state. Booked appointments, patients, and bills are written to MySQL. Session is per browser session and is temporary; it is not authentication.

## Testing

Run the business-rule check executable:

        dotnet run --project .\tests\BusinessRuleChecks\BusinessRuleChecks.csproj

It checks bill arithmetic, invalid monetary values, appointment slot rules, future time checks, and allowed status transitions. In this workspace it ran 10/10 checks successfully.

### Requirement-to-test matrix

The full implementation-location, verification, actual-status, and remaining-work report is in docs/requirement-compliance.md.

| Requirement / scenario | Implementation | Verification recorded | Status |
|---|---|---|---|
| MVC/Razor, DI, Session middleware | Program.cs, Views/ | dotnet build | Build verified |
| MySQL provider and EF model | Data/HospitalDbContext.cs, .csproj | Project restore/build; no local MySQL server | Build verified; connection pending |
| Initial migration | Migrations/ | C# compile only; dotnet ef database update not run | Implemented; DB pending |
| Bill arithmetic and invalid amounts | Services/BillCalculator.cs | BusinessRuleChecks, 10/10 pass | Verified |
| Appointment slot/time/status rules | Services/AppointmentRules.cs | BusinessRuleChecks, 10/10 pass | Verified |
| Patient/doctor/department CRUD against MySQL | Controllers and Razor views | Not run; requires MySQL | Manual verification pending |
| Appointment CRUD, conflict and foreign-key behavior | AppointmentsController, AppointmentService | Not run; requires MySQL | Manual verification pending |
| Bill generation and MySQL persistence | BillsController, BillingService | Arithmetic checks ran; DB write not run | Partial; DB pending |
| Session persists filters across requests | AppointmentsController | Middleware/build verified; browser session test not run | Manual verification pending |
| Dashboard uses actual data | HomeController | Query compiles; no MySQL records available | DB verification pending |
| Search, pagination, responsive screens | Module controllers, Razor and CSS | Build verified; browser/responsive review not run | Manual verification pending |
| No committed credentials or real patient information | .gitignore, appsettings template, seed data | Source review | Verified by inspection |

### Manual browser checks after MySQL setup

1. Register a fictional patient; submit an invalid email/phone and a future birth date to see server validation; then edit and deactivate it.
2. Create and edit departments and doctors; verify duplicate department names are rejected and assigned departments cannot be deleted.
3. Book an appointment in a valid future slot; attempt a past time, an off-hour time, an unavailable doctor, and the same doctor's occupied slot.
4. Confirm, reschedule, cancel, and complete appointments; confirm invalid status reversals and completing a future appointment are rejected.
5. Apply appointment filters, navigate pages or reload with the same browser session, verify filter values return, then clear filters. Repeat after 30 minutes idle to observe expiration.
6. Generate a bill for a completed appointment; verify the consultation fee, discount, configured tax, and total; reject a negative or excessive discount; print the invoice and mark it paid.
7. Compare the dashboard counts with records in MySQL. Resize the browser to desktop, tablet, and phone widths and review the major screens.

## Screenshots

No application screenshots are included because the application has not yet been connected to MySQL in this environment. Capture real screenshots after setup and save them under docs/screenshots/. Recommended captures:

1. Dashboard with actual counts and recent appointments.
2. Patient directory and patient detail.
3. Appointment list showing filters and statuses.
4. Appointment details after a successful booking.
5. Print-friendly invoice with server-calculated totals.
6. Mobile-width dashboard or appointment screen.

Do not include real patient data in screenshots. Use fictional demo records only.

## Technical poster

docs/poster/Poster-A2.html is an A2 portrait poster source with configurable student/institute placeholders and screenshot slots. It deliberately does not contain fabricated screenshots. Once real screenshots are added, open the HTML in Edge or Chrome and print/export with:

- Paper: A2, portrait, 420 mm × 594 mm.
- Margins: none (or minimum); scale: fit to page; enable background graphics.
- Export destination: PDF; inspect all text and screenshots at 100% before submission.

If the browser print dialog does not offer A2, use More settings → Paper size → A2 or the printer driver's A2 size; do not scale the A2 content onto a smaller paper size.

## Technical viva guide

1. **Why MVC?** Controllers coordinate requests, models represent persistent records, and Razor views render typed pages; the separation makes the workflow understandable and testable.
2. **What does EF Core do?** It maps C# entities and LINQ queries to MySQL tables and parameterized SQL, while migrations version schema changes.
3. **Why use ViewModels?** Forms accept only the fields needed for a specific action, reducing over-posting and separating UI concerns from database entities.
4. **How are duplicate records prevented?** The server checks inputs and MySQL unique indexes enforce uniqueness if two requests race.
5. **How does appointment conflict checking work?** A 30-minute slot rule is validated, the doctor's row is locked in a serializable MySQL transaction, and the slot is checked before saving.
6. **Why store appointment times in UTC?** UTC makes persistence and comparison consistent; input and display use the configured local time zone.
7. **What does Session store?** Temporary appointment filter values only. Permanent records are written to MySQL.
8. **How is a bill total computed?** The server loads the consultation fee and applies subtotal minus discount plus rounded tax; posted totals are never trusted.
9. **Why can't patient or doctor records be deleted freely?** Their appointments and bills are historical records; deactivation preserves foreign-key relationships.
10. **What are the limitations?** No authentication, live database verification, payment processing, or clinical decision support is included in this educational build.

## Known limitations and remaining verification

- No administrative authentication is implemented. All pages are accessible in the local demo; do not deploy with real patient information.
- This workspace has no running MySQL server or credentials. The app's database queries, migration application, CRUD persistence, and session workflow must be verified after configuring MySQL.
- The business-rule checks are executable checks, not a database integration test suite.
- The initial migration compiles but has not been applied through dotnet ef in this environment.
- No real application screenshots or final poster PDF are included yet.
- Appointment locking assumes the standard InnoDB engine and all booking requests use this application's service.
- This application is an academic administration demonstration, not a production healthcare system.

## GitHub submission

The .gitignore excludes build output, local IDE state, test results, and local secret files while retaining migrations, source code, tests, docs, and the tool manifest. No remote has been created and no code has been pushed.

After reviewing the project and configuring GitHub, run:

        git init
        git add .
        git commit -m "Initial Hospital Management System"
        git branch -M main
        git remote add origin https://github.com/<your-account>/<your-repository>.git
        git push -u origin main

Before pushing, review git status and confirm no local secrets or real records have been added.
