using System.Net;
using System.Text.Json;
using BookingSystem.Application.DTOs.Common;
using BookingSystem.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        string message = "An error occurred while processing your request.";
        List<string>? errors = null;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = HttpStatusCode.BadRequest;
                message = "One or more validation errors occurred.";
                errors = validationException.Errors.Select(e => e.ErrorMessage).ToList();
                break;

            case BusinessRuleException businessRuleException:
                statusCode = HttpStatusCode.BadRequest;
                message = businessRuleException.Message;
                break;

            case ConcurrencyException concurrencyException:
                statusCode = HttpStatusCode.Conflict;
                message = concurrencyException.Message;
                break;

            case DbUpdateConcurrencyException:
                statusCode = HttpStatusCode.Conflict;
                message = "The resource was modified by another user. Please refresh and try again.";
                break;

            case NotFoundException notFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = notFoundException.Message;
                break;

            case InvalidStatusTransitionException statusException:
                statusCode = HttpStatusCode.BadRequest;
                message = statusException.Message;
                break;

            case UnauthorizedException unauthorizedException:
                statusCode = HttpStatusCode.Forbidden;
                message = unauthorizedException.Message;
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = exception.Message;
                if (string.IsNullOrEmpty(message))
                    message = "Authentication is required to access this resource.";
                break;

            case KeyNotFoundException:
            case InvalidOperationException when exception.Message.Contains("not found"):
                statusCode = HttpStatusCode.NotFound;
                message = exception.Message;
                break;

            case ArgumentException:
            case InvalidOperationException:
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                break;

            default:
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.FailureResponse(message, errors);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response, options);
        return context.Response.WriteAsync(json);
    }
}
