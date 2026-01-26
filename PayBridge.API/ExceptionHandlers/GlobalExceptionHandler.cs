using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PayBridge.Domain.Exceptions;

namespace PayBridge.API.ExceptionHandlers
{
    public class GlobalExceptionHandler: IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            this.logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var problemDetails = exception switch
            {
                // Domain exceptions (are caught in service, but just in case...)
                DomainException domainEx => new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Business Rule Violation",
                    Detail = domainEx.Message,
                    Extensions = new Dictionary<string, object?>
                    {
                        { "errorCode", domainEx.ErrorCode },
                        { "traceId", httpContext.TraceIdentifier }
                    }
                },

                // Validation exceptions
                ArgumentNullException argNullEx => new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Validation Error",
                    Detail = argNullEx.Message,
                    Extensions = new Dictionary<string, object?>
                    {
                        { "parameterName", argNullEx.ParamName },
                        { "traceId", httpContext.TraceIdentifier }
                    }
                },

                ArgumentException argEx => new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Validation Error",
                    Detail = argEx.Message,
                    Extensions = new Dictionary<string, object?>
                    {
                        { "traceId", httpContext.TraceIdentifier }
                    }
                },

                // Not found exceptions
                KeyNotFoundException keyNotFoundEx => new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Resource Not Found",
                    Detail = keyNotFoundEx.Message,
                    Extensions = new Dictionary<string, object?>
                    {
                        { "traceId", httpContext.TraceIdentifier }
                    }
                },

                // Unauthorized exceptions
                UnauthorizedAccessException => new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Title = "Unauthorized",
                    Detail = "You are not authorized to access this resource",
                    Extensions = new Dictionary<string, object?>
                    {
                        { "traceId", httpContext.TraceIdentifier }
                    }
                },

                // All other exceptions (500 Internal Server Error)
                _ => new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred. Please try again later.",
                    Extensions = new Dictionary<string, object?>
                    {
                        { "traceId", httpContext.TraceIdentifier }
                    }
                }
            };

            LogException(exception, httpContext);

            // Set response
            httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true; 
        }

        private void LogException(Exception exception, HttpContext httpContext)
        {
            var logLevel = exception switch
            {
                DomainException => LogLevel.Warning,
                ArgumentException => LogLevel.Warning,
                KeyNotFoundException => LogLevel.Information,
                _ => LogLevel.Error
            };

            logger.Log(
                logLevel,
                exception,
                "Exception occurred while processing request {Method} {Path}. TraceId: {TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);
        }
    }
}
