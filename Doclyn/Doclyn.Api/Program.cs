using Doclyn.Api.Extensions;
using Doclyn.Api.Hangfire;
using Doclyn.Api.Configuration;
using Doclyn.Application;
using Doclyn.Application.Documents;
using Doclyn.Infrastructure;
using Doclyn.Infrastructure.Security;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

// Serilog bootstrap logger — captura erros de inicialização antes do host estar pronto
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Doclyn API");

    EnvironmentFileLoader.Load(Directory.GetCurrentDirectory());

    var builder = WebApplication.CreateBuilder(args);

    // --- Serilog: substitui o logging padrão do .NET pelo Serilog
    builder.Host.UseSerilog();

    // --- Services ---
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHttpContextAccessor();

    // --- Authentication & Authorization ---
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Jwt configuration not found.");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    // --- Rate Limiting ---
    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("ForgotPasswordPerIp", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromHours(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        options.AddPolicy("VerifyResetCodePerIp", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromHours(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(
                """{"type":"TooManyRequests","message":"Too many requests. Please try again later."}""",
                token);
        };
    });

    builder.Services.AddControllers();

    var documentOptions = builder.Configuration.GetSection(DocumentOptions.Section).Get<DocumentOptions>()
        ?? new DocumentOptions();
    var maxUploadSizeInBytes = documentOptions.MaxUploadSizeInMb * 1024L * 1024L;

    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = maxUploadSizeInBytes;
    });

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = maxUploadSizeInBytes;
    });

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
        {
            Title = "Doclyn API",
            Version = "v1",
            Description = "Document intelligence — PDF upload, OCR, AI extraction, processing history, dashboard."
        });

        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token: Bearer {your token}"
        });
    });

    builder.Services.AddHealthChecks();

    // --- App ---
    var app = builder.Build();

    app.UseGlobalExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Doclyn API v1");
        });
        app.UseHangfireDashboard("/hangfire");
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireDashboardAuthorizationFilter()]
        });
    }

    app.UseRateLimiter();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Doclyn API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
