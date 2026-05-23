using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SessionSentinel.Domain.Entities;

namespace SessionSentinel.Persistence.Configurations;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(x => x.FingerprintHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.UserAgent).HasMaxLength(1024);
        builder.Property(x => x.Language).HasMaxLength(128);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.LastRequestAtUtc).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => x.SessionId).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.IsActive });
    }
}
