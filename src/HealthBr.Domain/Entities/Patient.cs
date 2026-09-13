namespace HealthBr.Domain.Entities;

public sealed class Patient : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }
}
