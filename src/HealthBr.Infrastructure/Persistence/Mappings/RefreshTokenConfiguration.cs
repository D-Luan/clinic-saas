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

        // Lookup key for the refresh flow: tokens are unique SHA-256 hashes of
        // 256-bit random values (task 1.3; debt from task 1.2).
        builder.HasIndex(x => x.Token)
            .IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureBaseColumns();
    }
}
