using HealthBr.Infrastructure.MultiTenancy;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HealthBr.IntegrationTests.Persistence;

public sealed class UniqueIndexTests : DatabaseTestBase
{
    public UniqueIndexTests(SqlServerFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Appointment_DoctorStartTime_ShouldRejectDuplicateActiveAppointment()
    {
        var tenantId = Guid.NewGuid();
        var doctor = TestEntities.User(tenantId, "doctor@clinic.com");
        var patient = TestEntities.Patient(tenantId);
        var start = TestEntities.UtcNow;

        await using (var seed = CreateContext())
        {
            seed.Users.Add(doctor);
            seed.Patients.Add(patient);
            seed.Appointments.Add(TestEntities.Appointment(tenantId, patient.Id, doctor.Id, start));
            await seed.SaveChangesAsync();
        }

        await using (var context = CreateContext(tenant => tenant.SetTenantId(tenantId)))
        {
            context.Appointments.Add(TestEntities.Appointment(tenantId, patient.Id, doctor.Id, start));

            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            var sqlError = Assert.IsType<SqlException>(exception.InnerException);
            Assert.True(sqlError.Number is 2601 or 2627, $"unexpected SQL error number: {sqlError.Number}");
        }
    }

    [Fact]
    public async Task Appointment_DoctorStartTime_ShouldAllowRecreateAfterSoftDelete()
    {
        var tenantId = Guid.NewGuid();
        var doctor = TestEntities.User(tenantId, "doctor@clinic.com");
        var patient = TestEntities.Patient(tenantId);
        var start = TestEntities.UtcNow;

        await using (var seed = CreateContext())
        {
            seed.Users.Add(doctor);
            seed.Patients.Add(patient);
            seed.Appointments.Add(TestEntities.Appointment(tenantId, patient.Id, doctor.Id, start));
            await seed.SaveChangesAsync();
        }

        await using (var context = CreateContext(tenant => tenant.SetTenantId(tenantId)))
        {
            var deleted = await context.Appointments.IgnoreQueryFilters()
                .SingleAsync(appointment => appointment.DoctorId == doctor.Id);

            deleted.IsDeleted = true;
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(tenant => tenant.SetTenantId(tenantId)))
        {
            context.Appointments.Add(TestEntities.Appointment(tenantId, patient.Id, doctor.Id, start));
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(tenant => tenant.SetTenantId(tenantId)))
        {
            var visible = await context.Appointments.ToListAsync();
            Assert.Single(visible);

            var all = await context.Appointments.IgnoreQueryFilters().ToListAsync();
            Assert.Equal(2, all.Count);
            Assert.Equal(1, all.Count(appointment => appointment.IsDeleted));
            Assert.Equal(1, all.Count(appointment => !appointment.IsDeleted));
        }
    }

    [Fact]
    public async Task User_Email_ShouldBeUniquePerTenantAmongActiveUsers()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        const string email = "shared@clinic.com";

        await using (var seed = CreateContext())
        {
            seed.Users.AddRange(TestEntities.User(tenantA, email), TestEntities.User(tenantB, email));
            await seed.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            context.Users.Add(TestEntities.User(tenantA, email));

            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            var sqlError = Assert.IsType<SqlException>(exception.InnerException);
            Assert.True(sqlError.Number is 2601 or 2627, $"unexpected SQL error number: {sqlError.Number}");
        }

        await using (var context = CreateContext())
        {
            var deleted = await context.Users.IgnoreQueryFilters()
                .SingleAsync(user => user.TenantId == tenantA && user.Email == email);

            deleted.IsDeleted = true;
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(tenant => tenant.SetTenantId(tenantA)))
        {
            context.Users.Add(TestEntities.User(tenantA, email));
            await context.SaveChangesAsync();

            var active = await context.Users.ToListAsync();
            Assert.Single(active);
        }
    }
}
