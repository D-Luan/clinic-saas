using HealthBr.Domain.Entities;
using HealthBr.Infrastructure.MultiTenancy;
using HealthBr.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthBr.IntegrationTests.Persistence;

public sealed class ModelMappingTests
{
    private static HealthBrDbContext CreateContext()
    {
        // Building the model never opens a connection; the string only feeds
        // the SQL Server provider.
        var options = new DbContextOptionsBuilder<HealthBrDbContext>()
            .UseSqlServer("Server=localhost;Database=HealthBr")
            .Options;

        return new HealthBrDbContext(options, new TenantContext());
    }

    [Fact]
    public void Model_ShouldContainExactlyTheSevenSpecEntities()
    {
        using var context = CreateContext();

        var expected = new[]
        {
            typeof(Tenant), typeof(User), typeof(RefreshToken), typeof(Patient),
            typeof(DoctorSchedule), typeof(Appointment), typeof(ClinicalNote)
        }.OrderBy(type => type.Name).ToList();
        var actual = context.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .OrderBy(type => type.Name)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EveryEntity_ShouldHaveTenantQueryFilter()
    {
        using var context = CreateContext();

        Assert.All(
            context.Model.GetEntityTypes(),
            entityType => Assert.NotEmpty(entityType.GetDeclaredQueryFilters()));
    }

    [Fact]
    public void SoftDeleteFilter_ShouldApplyToEveryEntityExceptClinicalNote()
    {
        using var context = CreateContext();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var filters = entityType.GetDeclaredQueryFilters()
                .Select(filter => filter.Expression?.ToString() ?? string.Empty)
                .ToList();

            if (entityType.ClrType == typeof(ClinicalNote))
            {
                Assert.All(filters, filter => Assert.DoesNotContain(nameof(BaseEntity.IsDeleted), filter));
            }
            else
            {
                Assert.True(
                    filters.Any(filter => filter.Contains(nameof(BaseEntity.IsDeleted))),
                    $"expected an IsDeleted filter for {entityType.ClrType.Name}");
            }
        }
    }

    [Fact]
    public void Columns_ShouldFollowSpecColumnTypes()
    {
        using var context = CreateContext();

        AssertColumn<Appointment>(context, nameof(Appointment.StartTime), "datetime2(0)");
        AssertColumn<Appointment>(context, nameof(Appointment.EndTime), "datetime2(0)");
        AssertColumn<Appointment>(context, nameof(Appointment.PaidAt), "datetime2(0)");
        AssertColumn<Appointment>(context, nameof(Appointment.Price), "decimal(10,2)");
        AssertColumn<Patient>(context, nameof(Patient.DateOfBirth), "date");
        AssertColumn<DoctorSchedule>(context, nameof(DoctorSchedule.StartTime), "time");
        AssertColumn<DoctorSchedule>(context, nameof(DoctorSchedule.EndTime), "time");
        AssertColumn<ClinicalNote>(context, nameof(ClinicalNote.NoteText), "nvarchar(max)");
    }

    [Fact]
    public void FilteredUniqueIndexes_ShouldBeConfigured()
    {
        using var context = CreateContext();

        var appointmentIndex = FindIndex<Appointment>(context, nameof(Appointment.DoctorId), nameof(Appointment.StartTime));
        Assert.True(appointmentIndex.IsUnique);
        Assert.Equal("[IsDeleted] = 0", appointmentIndex.GetFilter());

        var userIndex = FindIndex<User>(context, nameof(User.TenantId), nameof(User.Email));
        Assert.True(userIndex.IsUnique);
        Assert.Equal("[IsDeleted] = 0", userIndex.GetFilter());
    }

    private static void AssertColumn<TEntity>(HealthBrDbContext context, string propertyName, string columnType)
        where TEntity : BaseEntity
    {
        var property = context.Model.FindEntityType(typeof(TEntity))!.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(columnType, property!.GetColumnType());
    }

    private static IReadOnlyIndex FindIndex<TEntity>(HealthBrDbContext context, params string[] properties)
        where TEntity : BaseEntity
    {
        return context.Model.FindEntityType(typeof(TEntity))!.GetIndexes()
            .Single(index => index.Properties.Select(property => property.Name).SequenceEqual(properties));
    }
}
