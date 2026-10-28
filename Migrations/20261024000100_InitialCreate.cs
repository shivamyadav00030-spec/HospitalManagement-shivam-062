using HospitalManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace HospitalManagementSystem.Migrations;

[DbContext(typeof(HospitalDbContext))]
[Migration("20261024000100_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Departments",
            columns: table => new
            {
                DepartmentId = table.Column<int>(type: "int", nullable: false).Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                DepartmentName = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Departments", x => x.DepartmentId));

        migrationBuilder.CreateTable(
            name: "Patients",
            columns: table => new
            {
                PatientId = table.Column<int>(type: "int", nullable: false).Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                PatientCode = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                FullName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                Gender = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                PhoneNumber = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: true),
                Address = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                RegistrationDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Patients", x => x.PatientId));

        migrationBuilder.CreateTable(
            name: "Doctors",
            columns: table => new
            {
                DoctorId = table.Column<int>(type: "int", nullable: false).Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                DoctorCode = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                FullName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                DepartmentId = table.Column<int>(type: "int", nullable: false),
                Specialization = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                PhoneNumber = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: true),
                ConsultationFee = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Doctors", x => x.DoctorId);
                table.ForeignKey("FK_Doctors_Departments_DepartmentId", x => x.DepartmentId, "Departments", "DepartmentId", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Appointments",
            columns: table => new
            {
                AppointmentId = table.Column<int>(type: "int", nullable: false).Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                AppointmentCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                PatientId = table.Column<int>(type: "int", nullable: false),
                DoctorId = table.Column<int>(type: "int", nullable: false),
                AppointmentDateTimeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                ReasonForVisit = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Appointments", x => x.AppointmentId);
                table.ForeignKey("FK_Appointments_Doctors_DoctorId", x => x.DoctorId, "Doctors", "DoctorId", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Appointments_Patients_PatientId", x => x.PatientId, "Patients", "PatientId", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Bills",
            columns: table => new
            {
                BillId = table.Column<int>(type: "int", nullable: false).Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                BillNumber = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                PatientId = table.Column<int>(type: "int", nullable: false),
                AppointmentId = table.Column<int>(type: "int", nullable: true),
                BillDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                Subtotal = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                DiscountAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                TaxRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                TaxAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                PaymentStatus = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false),
                Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bills", x => x.BillId);
                table.ForeignKey("FK_Bills_Appointments_AppointmentId", x => x.AppointmentId, "Appointments", "AppointmentId", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Bills_Patients_PatientId", x => x.PatientId, "Patients", "PatientId", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_Departments_DepartmentName", "Departments", "DepartmentName", unique: true);
        migrationBuilder.CreateIndex("IX_Patients_PatientCode", "Patients", "PatientCode", unique: true);
        migrationBuilder.CreateIndex("IX_Doctors_DoctorCode", "Doctors", "DoctorCode", unique: true);
        migrationBuilder.CreateIndex("IX_Doctors_DepartmentId", "Doctors", "DepartmentId");
        migrationBuilder.CreateIndex("IX_Appointments_AppointmentCode", "Appointments", "AppointmentCode", unique: true);
        migrationBuilder.CreateIndex("IX_Appointments_DoctorId_AppointmentDateTimeUtc", "Appointments", new[] { "DoctorId", "AppointmentDateTimeUtc" });
        migrationBuilder.CreateIndex("IX_Appointments_PatientId_AppointmentDateTimeUtc", "Appointments", new[] { "PatientId", "AppointmentDateTimeUtc" });
        migrationBuilder.CreateIndex("IX_Bills_BillNumber", "Bills", "BillNumber", unique: true);
        migrationBuilder.CreateIndex("IX_Bills_AppointmentId", "Bills", "AppointmentId", unique: true);
        migrationBuilder.CreateIndex("IX_Bills_PatientId", "Bills", "PatientId");

        migrationBuilder.InsertData(
            table: "Departments",
            columns: new[] { "DepartmentId", "DepartmentName", "Description", "IsActive" },
            values: new object[,]
            {
                { 1, "Cardiology", "Heart and circulatory care", true },
                { 2, "General Medicine", "General outpatient services", true },
                { 3, "Orthopedics", "Bone and joint services", true },
                { 4, "Pediatrics", "Child health services", true }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Bills");
        migrationBuilder.DropTable(name: "Appointments");
        migrationBuilder.DropTable(name: "Doctors");
        migrationBuilder.DropTable(name: "Patients");
        migrationBuilder.DropTable(name: "Departments");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder) => InitialMigrationModel.Build(modelBuilder);
}
