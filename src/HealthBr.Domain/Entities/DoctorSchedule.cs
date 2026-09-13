namespace HealthBr.Domain.Entities;

public sealed class DoctorSchedule : BaseEntity
{
    public Guid DoctorId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}
