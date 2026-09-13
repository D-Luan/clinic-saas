namespace HealthBr.Domain.Entities;

public sealed class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}
