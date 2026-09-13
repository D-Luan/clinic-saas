using System.Linq.Expressions;

using HealthBr.Application.Common.Interfaces;
using HealthBr.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace HealthBr.Infrastructure.Persistence;

public sealed class HealthBrDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public HealthBrDbContext(DbContextOptions<HealthBrDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HealthBrDbContext).Assembly);

        ApplyGlobalQueryFilters(modelBuilder);
    }

    // Spec 15.7: every BaseEntity descendant must carry the TenantId filter.
    // Soft delete (IsDeleted = false) applies to all of them except the
    // immutable ClinicalNote (spec 4.5). The filter reads _tenantContext from
    // the current context instance on every query execution.
    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType == typeof(BaseEntity) || !typeof(BaseEntity).IsAssignableFrom(clrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(clrType, "entity");
            var tenantId = Expression.Property(parameter, nameof(BaseEntity.TenantId));
            var currentTenantId = Expression.Property(
                Expression.Field(Expression.Constant(this), nameof(_tenantContext)),
                nameof(ITenantContext.TenantId));

            Expression body = Expression.Equal(tenantId, currentTenantId);

            if (clrType != typeof(ClinicalNote))
            {
                var isDeleted = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                body = Expression.AndAlso(body, Expression.Not(isDeleted));
            }

            modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }
}
