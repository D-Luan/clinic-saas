using HealthBr.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthBr.Infrastructure.Persistence.Mappings;

internal static class BaseEntityMappingExtensions
{
    // Spec 3.4: all DateTime columns are UTC datetime2(0).
    public static void ConfigureBaseColumns<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : BaseEntity
    {
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(0)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)");
    }
}
