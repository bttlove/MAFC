// Middlewares/ResponseWrappingMiddleware.cs
using pviBase.Dtos;
using pviBase.Helpers; // Cần cho ErrorCodes
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace pviBase.Middlewares
{
    public class ResponseWrappingMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseWrappingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                await _next(context);

                // Kiểm tra nếu là file download (content-type là file/binary) thì không wrap
                var contentType = context.Response.ContentType;
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseBytes = responseBody.ToArray();
                var responseText = string.Empty;
                if (responseBytes.Length > 0)
                {
                    responseText = Encoding.UTF8.GetString(responseBytes);
                }

                // Nếu là file download (octet-stream, pdf, image, zip, v.v.) thì không wrap
                if (!string.IsNullOrEmpty(contentType) &&
                    (contentType.StartsWith("application/octet-stream") ||
                     contentType.StartsWith("application/pdf") ||
                     contentType.StartsWith("image/") ||
                     contentType.StartsWith("application/zip") ||
                     contentType.StartsWith("application/vnd")))
                {
                    context.Response.Body = originalBodyStream;
                    context.Response.ContentLength = responseBytes.Length;
                    // Đảm bảo ghi đúng stream, không bị lỗi khi file lớn
                    await originalBodyStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    return;
                }

                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    bool isAlreadyWrapped = false;
                    try
                    {
                        if (!string.IsNullOrEmpty(responseText))
                        {
                            using (var jsonDoc = JsonDocument.Parse(responseText))
                            {
                                if (jsonDoc.RootElement.TryGetProperty("status", out _) &&
                                    jsonDoc.RootElement.TryGetProperty("code", out _) &&
                                    jsonDoc.RootElement.TryGetProperty("message", out _))
                                {
                                    isAlreadyWrapped = true;
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Not valid JSON or not our ApiResponse format, proceed with wrapping
                    }

                    if (!isAlreadyWrapped)
                    {
                        JsonElement dataElement;
                        if (string.IsNullOrEmpty(responseText))
                        {
                            dataElement = JsonDocument.Parse("{}" ).RootElement;
                        }
                        else
                        {
                            dataElement = JsonDocument.Parse(responseText).RootElement;
                        }

                        // Sử dụng mã thành công và thông báo từ ErrorCodes
                        var apiResponse = new ApiResponse<JsonElement>(
                            status: true,
                            code: ErrorCodes.SuccessCode, // Mã thành công
                            message: ErrorCodes.SuccessMessage, // Thông báo thành công
                            data: dataElement
                        );

                        var wrappedJson = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                        context.Response.ContentType = "application/json";
                        context.Response.ContentLength = Encoding.UTF8.GetBytes(wrappedJson).Length;

                        await originalBodyStream.WriteAsync(Encoding.UTF8.GetBytes(wrappedJson));
                    }
                    else
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.ContentLength = Encoding.UTF8.GetBytes(responseText).Length;
                        await originalBodyStream.WriteAsync(Encoding.UTF8.GetBytes(responseText));
                    }
                }
                else
                {
                    context.Response.ContentLength = Encoding.UTF8.GetBytes(responseText).Length;
                    await originalBodyStream.WriteAsync(Encoding.UTF8.GetBytes(responseText));
                }
            }
            }
        }
    }

    public static class ResponseWrappingMiddlewareExtensions
    {
        public static IApplicationBuilder UseResponseWrappingMiddleware(this IApplicationBuilder builder)
        {
            // Đảm bảo using Microsoft.AspNetCore.Builder;
            return builder.UseMiddleware<pviBase.Middlewares.ResponseWrappingMiddleware>();
        }
    }
