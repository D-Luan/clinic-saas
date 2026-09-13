using HealthBr.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthBr.Infrastructure.Persistence.Mappings;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.Property(x => x.StartTime).HasColumnType("datetime2(0)");

        builder.Property(x => x.EndTime).HasColumnType("datetime2(0)");

        builder.Property(x => x.Price).HasPrecision(10, 2);

        builder.Property(x => x.PaidAt).HasColumnType("datetime2(0)");

        // Spec 4.4: safety net against double-booking; the filter lets an
        // appointment be recreated after a soft delete.
        builder.HasIndex(x => new { x.DoctorId, x.StartTime })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureBaseColumns();
    }
}
