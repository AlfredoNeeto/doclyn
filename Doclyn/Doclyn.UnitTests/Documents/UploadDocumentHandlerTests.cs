using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Common.Options;
using Doclyn.Application.Documents;
using Doclyn.Application.Documents.Upload;
using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Doclyn.UnitTests.Documents;

public sealed class UploadDocumentHandlerTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly CurrentUserServiceMock _currentUser;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileHashService _fileHashService;
    private readonly IDocumentProcessingQueue _documentProcessingQueue;
    private readonly UploadDocumentHandler _handler;

    public UploadDocumentHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _currentUser = new CurrentUserServiceMock();
        _fileStorageService = Substitute.For<IFileStorageService>();
        _fileHashService = Substitute.For<IFileHashService>();
        _documentProcessingQueue = Substitute.For<IDocumentProcessingQueue>();

        var logger = Substitute.For<ILogger<UploadDocumentHandler>>();

        _handler = new UploadDocumentHandler(
            _context,
            _context,
            _fileStorageService,
            _fileHashService,
            _currentUser,
            _documentProcessingQueue,
            logger);
    }

    [Fact]
    public async Task Should_Create_Document_With_Status_Pending_And_Type_Unknown()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        _fileHashService.ComputeSha256Async(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("sha256-hash");

        _fileStorageService.UploadAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("documents/user/doc/original.pdf");

        var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF
        var command = new UploadDocumentCommand(stream, "file.pdf", "application/pdf", stream.Length);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("Pending", response.DocumentStatus);
        Assert.Equal(DocumentTypes.Unknown, response.DocumentType);

        var document = await _context.Documents.FindAsync(response.Id);
        Assert.NotNull(document);
        Assert.Equal(DocumentStatus.Pending, document.DocumentStatus);
        Assert.Equal(DocumentTypes.Unknown, document.DocumentType);
        Assert.Equal("file.pdf", document.FileName);
        Assert.Equal($"documents/{userId}/{response.Id}/original.pdf", document.StoragePath);
    }

    [Fact]
    public async Task Should_Create_Processing_Log_On_Upload()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        _fileHashService.ComputeSha256Async(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("sha256-hash");

        _fileStorageService.UploadAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("documents/user/doc/original.pdf");

        var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var command = new UploadDocumentCommand(stream, "file.pdf", "application/pdf", stream.Length);

        var response = await _handler.Handle(command, CancellationToken.None);

        var logs = _context.ProcessingLogs.Where(l => l.DocumentId == response.Id).ToList();
        Assert.Single(logs);
        Assert.Equal("Upload", logs[0].Step);
        Assert.Equal(DocumentStatus.Success, logs[0].Status);
    }

    [Fact]
    public async Task Should_Call_File_Storage_Service_With_Expected_Object_Name()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        _fileHashService.ComputeSha256Async(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("sha256-hash");

        _fileStorageService.UploadAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("ignored-by-handler");

        var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var command = new UploadDocumentCommand(stream, "invoice.final.pdf", "application/pdf", stream.Length);

        var response = await _handler.Handle(command, CancellationToken.None);

        await _fileStorageService.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            $"documents/{userId}/{response.Id}/original.pdf",
            "application/pdf",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Enqueue_Document_For_Processing()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        _fileHashService.ComputeSha256Async(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("sha256-hash");

        _fileStorageService.UploadAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("ignored-by-handler");

        var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var response = await _handler.Handle(
            new UploadDocumentCommand(stream, "file.pdf", "application/pdf", stream.Length),
            CancellationToken.None);

        _documentProcessingQueue.Received(1).Enqueue(response.Id);
    }

    [Fact]
    public async Task Should_Throw_When_User_Not_Authenticated()
    {
        _currentUser.UserId = null;

        var command = new UploadDocumentCommand(Stream.Null, "file.pdf", "application/pdf", 1024);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
