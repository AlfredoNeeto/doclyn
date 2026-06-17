using Doclyn.Application.Common.Exceptions;
using Doclyn.Application.Documents.Delete;
using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.UnitTests.Common;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.UnitTests.Documents;

public sealed class DeleteDocumentHandlerTests
{
    private readonly TestDbContext _context;
    private readonly CurrentUserServiceMock _currentUser;
    private readonly DeleteDocumentHandler _handler;

    public DeleteDocumentHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _currentUser = new CurrentUserServiceMock();
        _handler = new DeleteDocumentHandler(_context, _context, _currentUser);
    }

    [Fact]
    public async Task Should_Soft_Delete_Document_When_Owner()
    {
        var userId = Guid.NewGuid();
        var document = Document.Create(userId, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        await _handler.Handle(new DeleteDocumentCommand(document.Id), CancellationToken.None);

        var persistedDocument = await _context.Documents.IgnoreQueryFilters().SingleAsync(d => d.Id == document.Id);
        var deletionLog = await _context.ProcessingLogs.SingleAsync(l => l.DocumentId == document.Id && l.Step == "DocumentDeleted");

        Assert.True(persistedDocument.IsDeleted);
        Assert.Equal(userId, persistedDocument.DeletedByUserId);
        Assert.NotNull(persistedDocument.DeletedAt);
        Assert.Equal(DocumentStatus.Success, deletionLog.Status);
    }

    [Fact]
    public async Task Should_Throw_When_Operator_Deletes_Other_User_Document()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var document = Document.Create(ownerId, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _currentUser.UserId = otherUserId;
        _currentUser.Role = UserRole.Operator.ToString();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new DeleteDocumentCommand(document.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Allow_Admin_To_Delete_Any_Document()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var document = Document.Create(ownerId, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _currentUser.UserId = adminId;
        _currentUser.Role = UserRole.Admin.ToString();

        await _handler.Handle(new DeleteDocumentCommand(document.Id), CancellationToken.None);

        var persistedDocument = await _context.Documents.IgnoreQueryFilters().SingleAsync(d => d.Id == document.Id);
        Assert.True(persistedDocument.IsDeleted);
        Assert.Equal(adminId, persistedDocument.DeletedByUserId);
    }

    [Fact]
    public async Task Should_Throw_When_Document_Not_Found()
    {
        _currentUser.UserId = Guid.NewGuid();
        _currentUser.Role = UserRole.Operator.ToString();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeleteDocumentCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
