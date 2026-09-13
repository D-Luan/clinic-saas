using HealthBr.Domain.Entities;
using HealthBr.Domain.Enums;

namespace HealthBr.IntegrationTests.Persistence;

internal static class TestEntities
{
    public static readonly DateTime UtcNow = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    public static Tenant Tenant(Guid id, string name = "Clínica Teste") => new()
    {
        Id = id,
        TenantId = id,
        Name = name,
        CreatedAt = UtcNow,
        UpdatedAt = UtcNow
    };

    public static Patient Patient(Guid tenantId, string name = "Paciente Teste", bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        DateOfBirth = new DateOnly(1990, 5, 20),
        Phone = "+5511999999999",
        Email = null,
        CreatedAt = UtcNow,
        UpdatedAt = UtcNow,
        IsDeleted = isDeleted
    };

    public static User User(Guid tenantId, string email, bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = "Dra. Ana Souza",
        Email = email,
        PasswordHash = "not-a-real-hash",
        Role = UserRole.Doctor,
        CreatedAt = UtcNow,
        UpdatedAt = UtcNow,
        IsDeleted = isDeleted
    };

    public static Appointment Appointment(Guid tenantId, Guid patientId, Guid doctorId, DateTime start) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        PatientId = patientId,
        DoctorId = doctorId,
        StartTime = start,
        EndTime = start.AddMinutes(30),
        Status = AppointmentStatus.Scheduled,
        Price = 200.50m,
        IsPaid = false,
        PaidAt = null,
        CreatedAt = UtcNow,
        UpdatedAt = UtcNow
    };

    public static ClinicalNote Note(Guid tenantId, Guid appointmentId, bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        AppointmentId = appointmentId,
        NoteText = "Paciente relata melhora do quadro desde a última consulta.",
        DateRecorded = UtcNow,
        CreatedAt = UtcNow,
        UpdatedAt = UtcNow,
        IsDeleted = isDeleted
    };
}
