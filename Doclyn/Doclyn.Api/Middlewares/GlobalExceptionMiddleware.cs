using Doclyn.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace Doclyn.Api.Middlewares;

/// <summary>
/// Middleware global de tratamento de exceções.
/// Converte exceções de domínio/aplicação em respostas HTTP apropriadas.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleUnauthorizedExceptionAsync(context, ex);
        }
        catch (NotFoundException ex)
        {
            await HandleNotFoundExceptionAsync(context, ex);
        }
        catch (DocumentStorageException ex)
        {
            await HandleDocumentStorageExceptionAsync(context, ex);
        }
        catch (InvalidOperationException ex)
        {
            await HandleBadRequestExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleGenericExceptionAsync(context, ex);
        }
    }

    private static Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/json";

        var response = new
        {
            type = "ValidationError",
            message = exception.Message,
            errors = exception.Errors
        };

        return WriteJsonAsync(context, response);
    }

    private static Task HandleUnauthorizedExceptionAsync(HttpContext context, UnauthorizedAccessException exception)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new
        {
            type = "Unauthorized",
            message = exception.Message
        };

        return WriteJsonAsync(context, response);
    }

    private static Task HandleBadRequestExceptionAsync(HttpContext context, InvalidOperationException exception)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/json";

        var response = new
        {
            type = "BadRequest",
            message = exception.Message
        };

        return WriteJsonAsync(context, response);
    }

    private static Task HandleDocumentStorageExceptionAsync(HttpContext context, DocumentStorageException exception)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new
        {
            message = exception.Message
        };

        return WriteJsonAsync(context, response);
    }

    private static Task HandleNotFoundExceptionAsync(HttpContext context, NotFoundException exception)
    {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        context.Response.ContentType = "application/json";

        var response = new
        {
            type = "NotFound",
            message = exception.Message
        };

        return WriteJsonAsync(context, response);
    }

    private static Task HandleGenericExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new
        {
            type = "InternalServerError",
            message = "An unexpected error occurred."
        };

        return WriteJsonAsync(context, response);
    }

    private static Task WriteJsonAsync(HttpContext context, object response)
    {
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}
