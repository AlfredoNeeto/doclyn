using Doclyn.Application.Common.Interfaces;
using Doclyn.Domain.Entities;
using Doclyn.Infrastructure.DocumentClasses;
using Doclyn.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Doclyn.UnitTests.DocumentClasses;

public sealed class DocumentClassCatalogServiceTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly DocumentClassCatalogService _service;

    public DocumentClassCatalogServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        var logger = Substitute.For<ILogger<DocumentClassCatalogService>>();
        _service = new DocumentClassCatalogService(_context, _context, logger);
    }

    [Fact]
    public async Task Create_Should_Create_New_Document_Class()
    {
        var documentClass = await _service.GetOrCreateAsync(
            "NOVA_CLASSE",
            "GRUPO",
            "SUBGRUPO",
            "Descrição da nova classe.",
            CancellationToken.None);

        Assert.Equal("NOVA_CLASSE", documentClass.Name);
        Assert.Equal("GRUPO", documentClass.Group);
        Assert.Equal("SUBGRUPO", documentClass.SubGroup);
        Assert.False(documentClass.IsSystemDefined);
        Assert.True(documentClass.IsActive);

        var persisted = await _context.DocumentClasses.SingleAsync(dc => dc.Id == documentClass.Id);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task GetOrCreate_Should_Reuse_Existing_Document_Class()
    {
        var existing = DocumentClass.Create(
            "CLASSE_EXISTENTE",
            "GRUPO",
            "SUBGRUPO",
            "Descrição.",
            isSystemDefined: false);

        _context.DocumentClasses.Add(existing);
        await _context.SaveChangesAsync();

        var documentClass = await _service.GetOrCreateAsync(
            "classe_existente",
            "OUTRO_GRUPO",
            "OUTRO_SUBGRUPO",
            "Outra descrição.",
            CancellationToken.None);

        Assert.Equal(existing.Id, documentClass.Id);
        Assert.Equal("GRUPO", documentClass.Group);

        var count = await _context.DocumentClasses.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task FindByName_Should_Normalize_Name_Before_Query()
    {
        var existing = DocumentClass.Create(
            "CLASSE_NORMALIZADA",
            "GRUPO",
            "SUBGRUPO",
            "Descrição.",
            isSystemDefined: true);

        _context.DocumentClasses.Add(existing);
        await _context.SaveChangesAsync();

        var result = await _service.FindByNameAsync("classe_normalizada", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("CLASSE_NORMALIZADA", result.Name);
    }

    [Fact]
    public async Task RegisterExample_Should_Create_Example_Link()
    {
        var documentClass = DocumentClass.Create(
            "CLASSE_COM_EXEMPLO",
            "GRUPO",
            "SUBGRUPO",
            "Descrição.",
            isSystemDefined: true);

        var document = Document.Create(
            Guid.NewGuid(),
            "file.pdf",
            "hash",
            "documents/test/original.pdf",
            documentClass.Name);

        _context.DocumentClasses.Add(documentClass);
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        await _service.RegisterExampleAsync(
            documentClass.Id,
            document.Id,
            0.95m,
            CancellationToken.None);

        var example = await _context.DocumentClassExamples.SingleAsync();
        Assert.Equal(documentClass.Id, example.DocumentClassId);
        Assert.Equal(document.Id, example.DocumentId);
        Assert.Equal(0.95m, example.Confidence);
    }

    [Fact]
    public void Create_Should_Derive_DisplayName_Automatically()
    {
        var documentClass = DocumentClass.Create(
            "RELATORIO_TECNICO_PRELIMINAR",
            "ADMINISTRATIVO",
            "PROCESSO_ADMINISTRATIVO",
            "Descrição.",
            isSystemDefined: true);

        Assert.Equal("relatorio tecnico preliminar", documentClass.DisplayName);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
