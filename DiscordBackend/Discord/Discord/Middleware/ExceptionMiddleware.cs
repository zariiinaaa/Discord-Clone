using Discord.Core.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Discord.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException =>
                (StatusCodes.Status400BadRequest, "Validation xətası"),

            UnauthorizedException =>
                (StatusCodes.Status401Unauthorized, "Autentifikasiya xətası"),

            ForbiddenException =>
                (StatusCodes.Status403Forbidden, "Giriş qadağandır"),

            ConflictException =>
                (StatusCodes.Status409Conflict, "Məlumat konflikti"),

            KeyNotFoundException =>
                (StatusCodes.Status404NotFound, "Məlumat tapılmadı"),

            _ =>
                (StatusCodes.Status500InternalServerError, "Server xətası")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Gözlənilməyən server xətası baş verdi.");
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "Gözlənilməyən server xətası baş verdi."
                : exception.Message,
            Instance = context.Request.Path
        };

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error => error.ErrorMessage)
                        .ToArray());

            problemDetails.Extensions["errors"] = errors;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}