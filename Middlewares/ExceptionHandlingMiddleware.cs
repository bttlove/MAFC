using pviBase.Dtos;
using pviBase.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
namespace pviBase.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception has occurred.");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = new ApiResponse(false, "500", "An unexpected error occurred.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            switch (exception)
            {
                case ValidationException validationException:
                    response.Code = "400";
                    response.Message = "Validation failed.";
                    // Optionally, you can include validation errors in Data
                    // response.Data = validationException.Errors;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                // Add more custom exception types here if needed
                default:
                    // For production, you might not want to expose raw exception messages.
                    // response.Message = "An unexpected error occurred. Please try again later.";
                    break;
            }

            var result = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(result);
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
