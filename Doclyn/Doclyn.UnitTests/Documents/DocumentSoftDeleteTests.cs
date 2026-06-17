using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;

namespace Doclyn.UnitTests.Documents;

public sealed class DocumentSoftDeleteTests
{
    [Fact]
    public void Delete_Should_Mark_Document_As_Deleted()
    {
        var document = Document.Create(Guid.NewGuid(), "file.pdf", "hash", "path", DocumentTypes.Unknown);
        var deletedByUserId = Guid.NewGuid();

        document.Delete(deletedByUserId);

        Assert.True(document.IsDeleted);
        Assert.Equal(deletedByUserId, document.DeletedByUserId);
        Assert.NotNull(document.DeletedAt);
    }

    [Fact]
    public void Delete_Should_Be_Idempotent_For_Already_Deleted_Document()
    {
        var document = Document.Create(Guid.NewGuid(), "file.pdf", "hash", "path", DocumentTypes.Unknown);
        var firstDeletedByUserId = Guid.NewGuid();
        var secondDeletedByUserId = Guid.NewGuid();

        document.Delete(firstDeletedByUserId);
        var firstDeletedAt = document.DeletedAt;

        document.Delete(secondDeletedByUserId);

        Assert.True(document.IsDeleted);
        Assert.Equal(firstDeletedByUserId, document.DeletedByUserId);
        Assert.Equal(firstDeletedAt, document.DeletedAt);
    }

    [Fact]
    public void Restore_Should_Clear_Soft_Delete_Fields()
    {
        var document = Document.Create(Guid.NewGuid(), "file.pdf", "hash", "path", DocumentTypes.Unknown);

        document.Delete(Guid.NewGuid());

        document.Restore();

        Assert.False(document.IsDeleted);
        Assert.Null(document.DeletedAt);
        Assert.Null(document.DeletedByUserId);
    }
}
