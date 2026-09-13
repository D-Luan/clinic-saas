using HealthBr.Domain.Enums;

namespace HealthBr.Domain.Entities;

public sealed class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public AppointmentStatus Status { get; set; }

    public decimal Price { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? PaidAt { get; set; }
}
