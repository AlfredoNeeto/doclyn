using Doclyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Doclyn.Infrastructure.Database.Configurations;

public sealed class ExtractedDataConfiguration : IEntityTypeConfiguration<ExtractedData>
{
    public void Configure(EntityTypeBuilder<ExtractedData> builder)
    {
        builder.HasKey(ed => ed.Id);

        var dataProperty = builder.Property(ed => ed.Data)
            .IsRequired()
            .HasConversion(
                data => data.RootElement.GetRawText(),
                json => JsonDocument.Parse(json))
            .HasColumnType("jsonb");

        dataProperty.Metadata.SetValueComparer(new ValueComparer<JsonDocument>(
            (left, right) =>
                (left ?? JsonDocument.Parse("{}")).RootElement.GetRawText() ==
                (right ?? JsonDocument.Parse("{}")).RootElement.GetRawText(),
            document => (document ?? JsonDocument.Parse("{}")).RootElement.GetRawText().GetHashCode(),
            document => JsonDocument.Parse((document ?? JsonDocument.Parse("{}")).RootElement.GetRawText())));

        builder.Property(ed => ed.CreatedAt)
            .IsRequired();

        builder.HasOne(ed => ed.Document)
            .WithOne()
            .HasForeignKey<ExtractedData>(ed => ed.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ed => ed.DocumentId)
            .IsUnique();
    }
}
