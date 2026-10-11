using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Shared.Common;

namespace FamilyManagement.API.Middleware;

/// <summary>
/// Converts exceptions into the standard ApiResponse envelope. App exceptions carry safe
/// messages; everything else is logged in full and returned as a generic message.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client went away — nothing to report.
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, ApiResponse.Fail(e.Message)),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, ApiResponse.Fail(e.Message)),
            ForbiddenException e => (StatusCodes.Status403Forbidden, ApiResponse.Fail(e.Message)),
            ConflictException e => (StatusCodes.Status409Conflict, ApiResponse.Fail(e.Message)),
            BadRequestException e => (StatusCodes.Status400BadRequest, ApiResponse.Fail(e.Message, e.Errors)),
            _ => (StatusCodes.Status500InternalServerError,
                  ApiResponse.Fail("Something went wrong. Please try again."))
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }
}
