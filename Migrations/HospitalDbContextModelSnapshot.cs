using HospitalManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HospitalManagementSystem.Migrations;

[DbContext(typeof(HospitalDbContext))]
public sealed class HospitalDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => InitialMigrationModel.Build(modelBuilder);
}
