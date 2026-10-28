# Requirement compliance report

**Review date:** 24 October 2026

“Implemented” describes code present in the project. “Build verified” means the project compiled. “Verified” is reserved for behavior covered by executed checks. Database and browser workflows remain pending where the environment did not provide MySQL or a working EF CLI.

| Requirement | Implementation location | Verification method | Actual status | Remaining work |
|---|---|---|---|---|
| ASP.NET Core MVC, C#, Razor, dependency injection | Program.cs, Controllers/, Views/, HospitalManagementSystem.csproj | dotnet build | Build verified; 0 warnings and 0 errors | Run the application after MySQL setup |
| EF Core with the required MySQL provider | HospitalManagementSystem.csproj, Data/HospitalDbContext.cs, Program.cs | Package restore and build; Oracle package compatibility checked | Implemented; build verified | Verify a live MySQL connection |
| Persistent database named HospitalManagementDB | appsettings.json, Data/HospitalDbContext.cs | Configuration inspection | Configured with a placeholder connection string | Install/start MySQL and set local credentials |
| EF Core migration and seeded departments | Migrations/InitialCreate, Migrations/HospitalDbContextModelSnapshot.cs | Hand-authored migration source compiles; dotnet-ef unavailable | Included; not generated or applied through dotnet-ef | Restore dotnet-ef, compare/regenerate if needed, and run dotnet ef database update |
| Patient CRUD, search, pagination, validation | Controllers/PatientsController.cs, Views/Patients/, Models/Patient.cs | Build; source review | Implemented; build verified | Exercise create/read/update/deactivate against MySQL |
| Doctor CRUD, department assignment, fee validation | Controllers/DoctorsController.cs, Views/Doctors/, Models/Doctor.cs | Build; source review | Implemented; build verified | Exercise create/update/deactivate against MySQL |
| Department CRUD, unique names, safe deletion | Controllers/DepartmentsController.cs, Views/Departments/, Models/Department.cs | Build; source review | Implemented; build verified | Exercise duplicate/dependency behavior against MySQL |
| Database-backed dashboard statistics | Controllers/HomeController.cs, Views/Home/Index.cshtml | Build; query inspection | Implemented; build verified | Compare counts to live MySQL records |
| Appointment booking, validation, past-time rejection | Controllers/AppointmentsController.cs, Services/AppointmentService.cs | Build; business-rule checks cover slots and future-time helper | Implemented; business rules verified | Verify bookings and validation with MySQL/browser |
| Appointment conflicts and concurrent booking protection | Services/AppointmentService.cs, database indexes in migrations | Transaction/row-lock source review | Implemented; not database verified | Run conflict and concurrent booking scenarios on MySQL/InnoDB |
| Appointment status changes, cancellation, rescheduling | Services/AppointmentRules.cs, Services/AppointmentService.cs, Views/Appointments/ | Build and business-rule checks | Business transition rules verified | Verify persisted status changes in MySQL |
| Appointment search/filter/pagination and Session state | Controllers/AppointmentsController.cs, Services/SessionJsonExtensions.cs, Program.cs | Build; middleware/source inspection | Implemented; not browser verified | Test filters across requests, clear state, and 30-minute expiry |
| Server-calculated billing and monetary validation | Services/BillCalculator.cs, Services/BillingService.cs, Controllers/BillsController.cs | BusinessRuleChecks executable: 10/10 passed | Business-rule arithmetic verified | Verify bill persistence and totals from live appointments |
| Itemized, print-friendly invoice and payment status | Views/Bills/Details.cshtml, Views/Bills/Invoice.cshtml | Build | Implemented; build verified | Test print output in a browser |
| Search and pagination for patients, doctors, departments, appointments, bills | Module controllers and Index Razor views | Build; query inspection | Implemented; build verified | Verify results against live database records |
| Server validation, antiforgery, restricted entity binding | Input ViewModels, POST actions, Razor forms | Build; source inspection; monetary rules tested | Implemented; partial behavior verified | Perform manual invalid-form checks in browser |
| Responsive Bootstrap user interface | Views/Shared/_Layout.cshtml, wwwroot/css/site.css, wwwroot/js/site.js | Build | Implemented; visual behavior not reviewed in browser | Inspect desktop/tablet/mobile layouts |
| Database relationships and historical record protection | Data/HospitalDbContext.cs, Models/, Migrations/ | Build; mapping inspection | Implemented; DB constraints not applied here | Verify foreign keys on MySQL |
| Helpful errors and missing-record handling | Controllers/, Services/, Views/Shared/Error.cshtml | Build; source inspection | Implemented; runtime error paths not exercised | Verify missing IDs, DB outages, and duplicate requests |
| Session versus database persistence documented | Program.cs, AppointmentsController.cs, README.md | Build and documentation inspection | Implemented; browser persistence not verified | Follow Session manual check in README |
| Requirement-to-test matrix and manual test instructions | README.md, this report | Documentation review | Present | Execute pending browser/database scenarios |
| Automated checks | tests/BusinessRuleChecks/ | dotnet run; 10/10 checks passed | Verified for pure business rules only | Add MySQL integration checks when a server is available |
| Secrets excluded; fictional data only | appsettings.json, .gitignore, seeded departments | Source inspection | Verified by inspection | Review git status before publishing |
| Authentication (optional) | Not implemented | Not applicable | Optional feature omitted; all pages are open in local demo | Add ASP.NET Core Identity before any real-data deployment |
| Actual screenshots | docs/screenshots/README.md | No live app/database available | Pending | Capture the listed screens after MySQL setup |
| A2 portrait poster source and PDF | docs/poster/Poster-A2.html, README.md | HTML source inspection; no PDF generated | Source prepared with honest screenshot placeholders | Replace placeholders with real screenshots and export A2 PDF |
| GitHub readiness and upload instructions | .gitignore, README.md | Source inspection | Prepared; not pushed | Add user-owned remote and push only after review |
| Runs in Visual Studio Code with no build errors | Project source and README instructions | dotnet build, 0 warnings/errors | Build verified; runtime not verified | Configure MySQL, apply migration, and launch from VS Code |
