using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionSentinel.Domain.Entities;

namespace SessionSentinel.Persistence.Configurations;

public sealed class AnomalyLogConfiguration : IEntityTypeConfiguration<AnomalyLog>
{
    public void Configure(EntityTypeBuilder<AnomalyLog> builder)
    {
        builder.ToTable("AnomalyLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.TriggeredRule)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Details)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
    }
}
