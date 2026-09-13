using HealthBr.Infrastructure.MultiTenancy;

using Microsoft.EntityFrameworkCore;

namespace HealthBr.IntegrationTests.Persistence;

public sealed class QueryFilterTests : DatabaseTestBase
{
    public QueryFilterTests(SqlServerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task TenantFilter_ShouldIsolateDataBetweenTenants()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var seed = CreateContext())
        {
            seed.Tenants.AddRange(TestEntities.Tenant(tenantA, "Clínica A"), TestEntities.Tenant(tenantB, "Clínica B"));
            seed.Patients.AddRange(TestEntities.Patient(tenantA, "Paciente A"), TestEntities.Patient(tenantB, "Paciente B"));
            await seed.SaveChangesAsync();
        }

        await using (var tenantAContext = CreateContext(tenant => tenant.SetTenantId(tenantA)))
        {
            var patient = Assert.Single(await tenantAContext.Patients.ToListAsync());
            Assert.Equal("Paciente A", patient.Name);

            var tenant = Assert.Single(await tenantAContext.Tenants.ToListAsync());
            Assert.Equal(tenantA, tenant.Id);
        }

        await using (var tenantBContext = CreateContext(tenant => tenant.SetTenantId(tenantB)))
        {
            var patient = Assert.Single(await tenantBContext.Patients.ToListAsync());
            Assert.Equal("Paciente B", patient.Name);
        }
    }

    [Fact]
    public async Task SoftDeleteFilter_ShouldHideDeletedPatients()
    {
        var tenantId = Guid.NewGuid();

        await using (var seed = CreateContext())
        {
            seed.Patients.AddRange(
                TestEntities.Patient(tenantId, "Ativo"),
                TestEntities.Patient(tenantId, "Removido", isDeleted: true));
            await seed.SaveChangesAsync();
        }

        await using (var context = CreateContext(tenant => tenant.SetTenantId(tenantId)))
        {
            var visible = await context.Patients.ToListAsync();
            Assert.Equal("Ativo", Assert.Single(visible).Name);
        }

        await using (var context = CreateContext(tenant => tenant.SetTenantId(tenantId)))
        {
            var all = await context.Patients.IgnoreQueryFilters().ToListAsync();
            Assert.Equal(2, all.Count);
        }
    }

    [Fact]
    public async Task ClinicalNote_ShouldNotBeHiddenBySoftDelete()
    {
        var tenantId = Guid.NewGuid();
        var doctor = TestEntities.User(tenantId, "doctor@clinic.com");
        var patient = TestEntities.Patient(tenantId);
        var appointment = TestEntities.Appointment(tenantId, patient.Id, doctor.Id, TestEntities.UtcNow);
        var note = TestEntities.Note(tenantId, appointment.Id, isDeleted: true);

        await using (var seed = CreateContext())
        {
            seed.Users.Add(doctor);
            seed.Patients.Add(patient);
            seed.Appointments.Add(appointment);
            seed.ClinicalNotes.Add(note);
            await seed.SaveChangesAsync();
        }

        await using (var context = CreateContext(tenant => tenant.SetTenantId(tenantId)))
        {
            var loaded = Assert.Single(await context.ClinicalNotes.ToListAsync());
            Assert.Equal(note.Id, loaded.Id);
        }
    }

    [Fact]
    public async Task UnsetTenantContext_ShouldReturnNoRows()
    {
        var tenantId = Guid.NewGuid();

        await using (var seed = CreateContext())
        {
            seed.Patients.Add(TestEntities.Patient(tenantId));
            await seed.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            Assert.Empty(await context.Patients.ToListAsync());
            Assert.Empty(await context.Tenants.ToListAsync());
        }
    }
}
