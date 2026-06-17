using Doclyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doclyn.Infrastructure.Database.Configurations;

public sealed class PasswordResetRequestConfiguration : IEntityTypeConfiguration<PasswordResetRequest>
{
    public void Configure(EntityTypeBuilder<PasswordResetRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.CodeHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(r => r.ResetTokenHash)
            .HasMaxLength(128);

        builder.HasIndex(r => r.ResetTokenHash)
            .IsUnique()
            .HasFilter("\"RESET_TOKEN_HASH\" <> ''");

        builder.Property(r => r.ExpiresAt)
            .IsRequired();

        builder.Property(r => r.ResetTokenExpiresAt);

        builder.Property(r => r.Attempts)
            .IsRequired();

        builder.Property(r => r.IsUsed)
            .IsRequired();

        builder.Property(r => r.IsResetTokenUsed)
            .IsRequired();

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.UserId);

        builder.Property(r => r.CreatedAt)
            .IsRequired();
    }
}
