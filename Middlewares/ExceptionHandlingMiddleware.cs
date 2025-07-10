// Middlewares/ExceptionHandlingMiddleware.cs
using pviBase.Dtos;
using pviBase.Helpers; // Cần cho ErrorCodes
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
            // Sử dụng mã lỗi và thông báo mặc định cho lỗi không xác định
            var response = new ApiResponse(false, ErrorCodes.ExceptionErrorsCode, ErrorCodes.UnexpectedErrorMessage);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            switch (exception)
            {
                case ValidationException validationException:
                    response.Code = ErrorCodes.InvalidParametersCode; // Mã lỗi cho tham số không hợp lệ
                    response.Message = ErrorCodes.InvalidParametersMessage; // Thông báo lỗi
                    // Bạn có thể bao gồm chi tiết lỗi xác thực trong trường Data nếu muốn
                    // response.Data = validationException.Errors;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                // Thêm các loại ngoại lệ tùy chỉnh khác ở đây nếu cần và gán mã lỗi tương ứng
                default:
                    // Đối với các lỗi không xác định, giữ mã 500 và thông báo chung
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