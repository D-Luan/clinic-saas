using HealthBr.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthBr.Infrastructure.Persistence.Mappings;

public sealed class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("DoctorSchedules");

        // Spec 3.4/4.3: times are TimeOnly in the tenant's local timezone
        // (America/Sao_Paulo) — never converted to UTC.
        builder.Property(x => x.StartTime).HasColumnType("time");

        builder.Property(x => x.EndTime).HasColumnType("time");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureBaseColumns();
    }
}
