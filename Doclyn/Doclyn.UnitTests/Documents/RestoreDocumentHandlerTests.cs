using Doclyn.Application.Common.Exceptions;
using Doclyn.Application.Documents.Restore;
using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.UnitTests.Common;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.UnitTests.Documents;

public sealed class RestoreDocumentHandlerTests
{
    private readonly TestDbContext _context;
    private readonly CurrentUserServiceMock _currentUser;
    private readonly RestoreDocumentHandler _handler;

    public RestoreDocumentHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _currentUser = new CurrentUserServiceMock();
        _handler = new RestoreDocumentHandler(_context, _context, _currentUser);
    }

    [Fact]
    public async Task Should_Restore_Soft_Deleted_Document_When_Admin()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var document = Document.Create(ownerId, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        document.Delete(ownerId);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _currentUser.UserId = adminId;
        _currentUser.Role = UserRole.Admin.ToString();

        await _handler.Handle(new RestoreDocumentCommand(document.Id), CancellationToken.None);

        var persistedDocument = await _context.Documents.IgnoreQueryFilters().SingleAsync(d => d.Id == document.Id);
        var restoreLog = await _context.ProcessingLogs.SingleAsync(l => l.DocumentId == document.Id && l.Step == "DocumentRestored");

        Assert.False(persistedDocument.IsDeleted);
        Assert.Null(persistedDocument.DeletedAt);
        Assert.Null(persistedDocument.DeletedByUserId);
        Assert.Equal(DocumentStatus.Success, restoreLog.Status);
    }

    [Fact]
    public async Task Should_Throw_When_Restore_Is_Requested_By_Non_Admin()
    {
        var ownerId = Guid.NewGuid();
        var document = Document.Create(ownerId, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        document.Delete(ownerId);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _currentUser.UserId = ownerId;
        _currentUser.Role = UserRole.Operator.ToString();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new RestoreDocumentCommand(document.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Deleted_Document_Not_Found()
    {
        _currentUser.UserId = Guid.NewGuid();
        _currentUser.Role = UserRole.Admin.ToString();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RestoreDocumentCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
