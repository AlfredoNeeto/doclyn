using Doclyn.Application.Common.Interfaces;
using Doclyn.Application.Common.Options;
using Doclyn.Application.Documents;
using Doclyn.Infrastructure.AI;
using Doclyn.Infrastructure.Classification;
using Doclyn.Infrastructure.Database;
using Doclyn.Infrastructure.DocumentClasses;
using Doclyn.Infrastructure.Email;
using Doclyn.Infrastructure.Extraction;
using Doclyn.Infrastructure.Insights;
using Doclyn.Infrastructure.Jobs;
using Doclyn.Infrastructure.OCR;
using Doclyn.Infrastructure.PDF;
using Doclyn.Infrastructure.Processing;
using Doclyn.Infrastructure.Security;
using Doclyn.Infrastructure.Storage;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Doclyn.Infrastructure.DocumentClassIndexers;
using Doclyn.Infrastructure.Validation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using Serilog;

namespace Doclyn.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found.");

        services
            .AddDatabase(connectionString)
            .AddSecurity(configuration)
            .AddEmail(configuration)
            .AddStorage(configuration)
            .AddHangfireServices(connectionString)
            .AddDocumentProcessing(configuration)
            .AddSerilogLogging(configuration);

        return services;
    }

    private static IServiceCollection AddDocumentProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.Section));
        services.Configure<OcrOptions>(configuration.GetSection(OcrOptions.Section));
        services.Configure<ClassificationOptions>(configuration.GetSection(ClassificationOptions.Section));
        services.Configure<FieldConfidenceOptions>(configuration.GetSection(FieldConfidenceOptions.Section));
        services.Configure<InsightOptions>(configuration.GetSection(InsightOptions.Section));
        services.TryAddScoped<OcrProcessingContextAccessor>();
        services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
        services.AddScoped<IDocumentClassCatalogService, DocumentClassCatalogService>();
        services.AddScoped<IDocumentClassIndexerCatalogService, DocumentClassIndexerCatalogService>();
        services.AddScoped<IDocumentSemanticClassificationService, SemanticClassificationService>();
        services.AddScoped<IClassGuidedExtractionService, ClassGuidedExtractionService>();
        services.AddScoped<IFieldValidationService, FieldValidationService>();
        services.AddScoped<IRuleBasedInsightGenerator, RuleBasedInsightGenerator>();
        services.AddScoped<IAiInsightGenerator, AiInsightGenerator>();
        services.AddScoped<IInsightMergeService, InsightMergeService>();
        services.AddScoped<IDocumentInsightService, DocumentInsightService>();
        services.AddHostedService<DocumentClassCatalogSeeder>();
        services.AddHostedService<DocumentClassIndexerSeeder>();
        services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
        services.AddScoped<IPdfToImageConverter, PdfToImageConverter>();
        services.AddScoped<OpenAiClientFactory>();
        services.AddScoped<PromptBuilder>();
        services.AddScoped<DocumentClassAiSchemaBuilder>();
        services.AddScoped<ExtractionMergeService>();
        services.AddScoped<DocumentTextMergeService>();
        services.AddScoped<IDocumentClassifier, RuleBasedDocumentClassifier>();
        services.AddScoped<IDocumentIndexer, RegexDocumentIndexer>();
        services.AddScoped<IAiDocumentClassifier, OpenAiDocumentClassifier>();
        services.AddScoped<IExtractionPromptBuilder, DynamicExtractionPromptBuilder>();
        services.AddScoped<IAiStructuredDataExtractor, OpenAiStructuredDataExtractor>();
        services.AddScoped<OpenAiSemanticClassifier>();
        services.AddScoped<IOcrService>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OcrOptions>>().Value;
            return options.Enabled
                ? sp.GetRequiredService<TesseractOcrService>()
                : sp.GetRequiredService<NotImplementedOcrService>();
        });
        services.AddScoped<TesseractOcrService>();
        services.AddScoped<NotImplementedOcrService>();
        services.AddScoped<ProcessDocumentJob>();

        return services;
    }

    private static IServiceCollection AddHangfireServices(this IServiceCollection services, string connectionString)
    {
        GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3 });

        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
            {
                PrepareSchemaIfNecessary = true
            }));

        services.AddHangfireServer();
        services.AddScoped<IDocumentProcessingQueue, HangfireDocumentProcessingQueue>();

        return services;
    }

    // -------------------------------------------------------------------------
    // EF Core + PostgreSQL
    // -------------------------------------------------------------------------
    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<DoclynDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(DoclynDbContext).Assembly.FullName)));

        // Registra os contratos da Application apontando para a mesma instância com escopo
        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<DoclynDbContext>());

        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<DoclynDbContext>());

        return services;
    }

    // -------------------------------------------------------------------------
    // Segurança: JWT, PasswordHasher, CurrentUser
    // -------------------------------------------------------------------------
    private static IServiceCollection AddSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.Section));

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }

    // -------------------------------------------------------------------------
    // E-mail: SMTP
    // -------------------------------------------------------------------------
    private static IServiceCollection AddEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SmtpOptions>(
            configuration.GetSection(SmtpOptions.Section));

        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }

    // -------------------------------------------------------------------------
    // Storage de objetos (MinIO)
    // -------------------------------------------------------------------------
    private static IServiceCollection AddStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StorageOptions>(
            configuration.GetSection(StorageOptions.Section));

        services.Configure<DocumentOptions>(
            configuration.GetSection(DocumentOptions.Section));

        var options = configuration
            .GetSection(StorageOptions.Section)
            .Get<StorageOptions>() ?? new StorageOptions();

        services.AddMinio(client => client
            .WithEndpoint(options.Endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey)
            .WithSSL(options.UseSsl));

        services.AddScoped<IFileStorageService, MinioFileStorageService>();
        services.AddScoped<IFileHashService, FileHashService>();

        return services;
    }

    // -------------------------------------------------------------------------
    // Serilog
    // -------------------------------------------------------------------------
    private static IServiceCollection AddSerilogLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ReadFrom.Configuration lê sinks, enrichers e níveis de appsettings.json
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        services.AddSerilog();

        return services;
    }
}
