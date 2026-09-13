namespace HealthBr.Domain.Entities;

// Immutable clinical record (spec 4.5): soft delete does not apply. TenantId
// isolation still applies via the Global Query Filter.
public sealed class ClinicalNote : BaseEntity
{
    public Guid AppointmentId { get; set; }

    public string NoteText { get; set; } = string.Empty;

    public DateTime DateRecorded { get; set; }
}
