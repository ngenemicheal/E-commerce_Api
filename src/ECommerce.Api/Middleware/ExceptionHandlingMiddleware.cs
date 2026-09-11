using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerce.Application.Exceptions;
using ECommerce.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var problem = exception switch
        {
            ValidationException validation => CreateValidationProblem(validation, traceId),
            NotFoundException notFound => CreateProblem(StatusCodes.Status404NotFound,
                notFound.Message, "Not Found", traceId),
            ForbiddenException forbidden => CreateProblem(StatusCodes.Status403Forbidden,
                forbidden.Message, "Forbidden", traceId),
            UnauthorizedException unauthorized => CreateProblem(StatusCodes.Status401Unauthorized,
                unauthorized.Message, "Unauthorized", traceId),
            ConflictException conflict => CreateProblem(StatusCodes.Status409Conflict,
                conflict.Message, "Conflict", traceId),
            DomainException domain => CreateProblem(StatusCodes.Status500InternalServerError,
                domain.Message, "Domain Error", traceId),
            _ => CreateProblem(StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later.",
                "Internal Server Error", traceId)
        };

        context.Response.StatusCode = problem.Status!.Value;
        context.Response.ContentType = "application/problem+json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(problem, options);
        await context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateProblem(int statusCode, string detail, string title, string traceId)
    {
        var pd = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = statusCode switch
            {
                StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
                StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            }
        };

        pd.Extensions["traceId"] = traceId;
        return pd;
    }

    private static ProblemDetails CreateValidationProblem(ValidationException ex, string traceId)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var kv in ex.Errors)
        {
            var key = string.IsNullOrWhiteSpace(kv.Key) ? string.Empty : kv.Key;
            errors[key] = kv.Value.ToArray();
        }

        var pd = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Detail = "See the 'errors' field for details.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };

        pd.Extensions["traceId"] = traceId;
        return pd;
    }
}
