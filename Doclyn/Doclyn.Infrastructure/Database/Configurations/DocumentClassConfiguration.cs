using Doclyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doclyn.Infrastructure.Database.Configurations;

public sealed class DocumentClassConfiguration : IEntityTypeConfiguration<DocumentClass>
{
    public void Configure(EntityTypeBuilder<DocumentClass> builder)
    {
        builder.HasKey(dc => dc.Id);

        builder.Property(dc => dc.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(dc => dc.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(dc => dc.Group)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("GroupName");

        builder.Property(dc => dc.SubGroup)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(dc => dc.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(dc => dc.IsSystemDefined)
            .IsRequired();

        builder.Property(dc => dc.IsActive)
            .IsRequired();

        builder.Property(dc => dc.CreatedAt)
            .IsRequired();

        builder.HasIndex(dc => dc.Name)
            .IsUnique();

        builder.HasIndex(dc => dc.Group);
        builder.HasIndex(dc => dc.SubGroup);
        builder.HasIndex(dc => dc.IsActive);
    }
}
