using SchoolAdmission.Domain.Exceptions;
using System.Text.Json;

namespace SchoolAdmission.Api
{
    public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (AppException ex)
            {
                await WriteErrorAsync(context, ex.StatusCode, ex.Message);
            }
            catch (BadHttpRequestException)
            {
                await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "Invalid request payload.");
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                context.Response.StatusCode = 499;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "An unexpected server error occurred.");
            }
        }

        private static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
        {
            if (context.Response.HasStarted)
            {
                return Task.CompletedTask;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new { error = message });
            return context.Response.WriteAsync(payload);
        }
    }

}
