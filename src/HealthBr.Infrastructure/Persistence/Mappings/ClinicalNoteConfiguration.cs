using HealthBr.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthBr.Infrastructure.Persistence.Mappings;

public sealed class ClinicalNoteConfiguration : IEntityTypeConfiguration<ClinicalNote>
{
    public void Configure(EntityTypeBuilder<ClinicalNote> builder)
    {
        builder.ToTable("ClinicalNotes");

        builder.Property(x => x.NoteText)
            .IsRequired();

        builder.Property(x => x.DateRecorded).HasColumnType("datetime2(0)");

        builder.HasOne<Appointment>()
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureBaseColumns();
    }
}
