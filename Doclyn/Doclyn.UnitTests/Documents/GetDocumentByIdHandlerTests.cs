using Doclyn.Application.Documents.GetById;
using Doclyn.Application.Common.Exceptions;
using Doclyn.Domain.Constants;
using Doclyn.Domain.Entities;
using Doclyn.Domain.Enums;
using Doclyn.UnitTests.Common;
using Microsoft.EntityFrameworkCore;

namespace Doclyn.UnitTests.Documents;

public sealed class GetDocumentByIdHandlerTests
{
    private readonly TestDbContext _context;
    private readonly CurrentUserServiceMock _currentUser;
    private readonly GetDocumentByIdHandler _handler;

    public GetDocumentByIdHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _currentUser = new CurrentUserServiceMock();
        _handler = new GetDocumentByIdHandler(_context, _currentUser);
    }

    [Fact]
    public async Task Should_Return_Document_When_Owner_Operator()
    {
        var userId = Guid.NewGuid();
        var document = Document.Create(userId, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _currentUser.UserId = userId;
        _currentUser.Role = UserRole.Operator.ToString();

        var response = await _handler.Handle(new GetDocumentByIdQuery(document.Id), CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(document.Id, response.Id);
    }

    [Fact]
    public async Task Should_Throw_When_Operator_Accesses_Others_Document()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var document = Document.Create(ownerId, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _currentUser.UserId = otherUserId;
        _currentUser.Role = UserRole.Operator.ToString();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new GetDocumentByIdQuery(document.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Return_Any_Document_When_Admin()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var document = Document.Create(ownerId, "file.pdf", "hash", "path", DocumentTypes.Unknown);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _currentUser.UserId = adminId;
        _currentUser.Role = UserRole.Admin.ToString();

        var response = await _handler.Handle(new GetDocumentByIdQuery(document.Id), CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(document.Id, response.Id);
    }

    [Fact]
    public async Task Should_Throw_When_Document_Not_Found()
    {
        _currentUser.UserId = Guid.NewGuid();
        _currentUser.Role = UserRole.Operator.ToString();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetDocumentByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
