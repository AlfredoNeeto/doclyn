using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Process;
using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Doclyn.UnitTests.Documents.Process;

public sealed class ProcessDocumentHandlerTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly CurrentUserServiceMock _currentUser;
    private readonly IDocumentProcessingQueue _documentProcessingQueue;
    private readonly ProcessDocumentHandler _handler;

    public ProcessDocumentHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _currentUser = new CurrentUserServiceMock();
        _documentProcessingQueue = Substitute.For<IDocumentProcessingQueue>();
        _handler = new ProcessDocumentHandler(_context, _currentUser, _documentProcessingQueue);
    }

    [Fact]
    public async Task Should_Enqueue_Document_Without_Changing_Status_To_Processing()
    {
        var userId = Guid.NewGuid();
        var document = Document.Create(userId, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        var response = await _handler.Handle(new ProcessDocumentCommand(document.Id), CancellationToken.None);

        Assert.Equal("Processing", response.Status);
        Assert.Null(response.DocumentType);

        var persistedDocument = await _context.Documents.SingleAsync(d => d.Id == document.Id);
        Assert.Equal(DocumentStatus.Pending, persistedDocument.DocumentStatus);

        _documentProcessingQueue.Received(1).Enqueue(document.Id);
    }

    [Fact]
    public async Task Should_Allow_Reprocessing_When_Document_Failed()
    {
        var userId = Guid.NewGuid();
        var document = Document.Create(userId, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        document.UpdateStatus(DocumentStatus.Failed);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        var response = await _handler.Handle(new ProcessDocumentCommand(document.Id), CancellationToken.None);

        Assert.Equal("Processing", response.Status);
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_Document_Does_Not_Exist()
    {
        _currentUser.UserId = Guid.NewGuid();
        _currentUser.Role = UserRole.Operator.ToString();

        await Assert.ThrowsAsync<Doclyn.Application.Common.Exceptions.NotFoundException>(() =>
            _handler.Handle(new ProcessDocumentCommand(Guid.NewGuid()), CancellationToken.None));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
