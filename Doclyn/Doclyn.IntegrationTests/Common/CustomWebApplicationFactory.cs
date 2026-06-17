using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Documents.Processing;
using Doclyn.Domain.Constants;
using Doclyn.Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Doclyn.IntegrationTests.Common;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public IFileStorageService FileStorageService { get; } = Substitute.For<IFileStorageService>();
    public IPdfTextExtractor PdfTextExtractor { get; } = Substitute.For<IPdfTextExtractor>();
    public IOcrService OcrService { get; } = Substitute.For<IOcrService>();
    public IAiDocumentClassifier AiDocumentClassifier { get; } = Substitute.For<IAiDocumentClassifier>();
    public IAiStructuredDataExtractor AiStructuredDataExtractor { get; } = Substitute.For<IAiStructuredDataExtractor>();
    public IDocumentSemanticClassificationService DocumentSemanticClassificationService { get; } = CreateDefaultSemanticClassifier();

    private static IDocumentSemanticClassificationService CreateDefaultSemanticClassifier()
    {
        var svc = Substitute.For<IDocumentSemanticClassificationService>();
        svc.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SemanticClassificationResult(
                DocumentClassId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DocumentType: DocumentTypes.RelatorioTecnicoPreliminar,
                Group: "ADMINISTRATIVO",
                SubGroup: "PROCESSO_ADMINISTRATIVO",
                Confidence: 0.95m,
                ReusedExistingClass: true,
                NewClassSuggested: false));
        return svc;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=doclyn_tests;Username=postgres;Password=changeme"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IFileStorageService));
            services.AddSingleton(FileStorageService);

            services.RemoveAll(typeof(IPdfTextExtractor));
            services.AddSingleton(PdfTextExtractor);

            services.RemoveAll(typeof(IOcrService));
            services.AddSingleton(OcrService);

            services.RemoveAll(typeof(IAiDocumentClassifier));
            services.AddSingleton(AiDocumentClassifier);

            services.RemoveAll(typeof(IAiStructuredDataExtractor));
            services.AddSingleton(AiStructuredDataExtractor);

            services.RemoveAll(typeof(IDocumentProcessingQueue));
            services.AddSingleton<IDocumentProcessingQueue, ImmediateDocumentProcessingQueue>();

            services.RemoveAll(typeof(IDocumentSemanticClassificationService));
            services.AddSingleton(DocumentSemanticClassificationService);
        });
    }
}
