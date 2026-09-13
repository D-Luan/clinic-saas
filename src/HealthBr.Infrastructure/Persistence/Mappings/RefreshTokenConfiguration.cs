using HealthBr.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthBr.Infrastructure.Persistence.Mappings;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.ExpiresAt).HasColumnType("datetime2(0)");

        builder.Property(x => x.RevokedAt).HasColumnType("datetime2(0)");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureBaseColumns();
    }
}
