// Middlewares/ResponseWrappingMiddleware.cs
using pviBase.Dtos;
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
            var originalBodyStream = context.Response.Body; // Lưu trữ luồng phản hồi gốc

            // Tạo một luồng bộ nhớ tạm thời để "chụp" phản hồi
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody; // Chuyển hướng luồng phản hồi đến luồng bộ nhớ tạm thời

                await _next(context); // Chuyển yêu cầu cho middleware tiếp theo (hoặc Controller)

                // Đặt con trỏ về đầu luồng bộ nhớ tạm thời
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync(); // Đọc toàn bộ nội dung phản hồi

                // Chỉ bọc nếu đó là phản hồi thành công (mã trạng thái 2xx)
                // và không phải là phản hồi rỗng hoàn toàn từ Ok(new {})
                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    // Thử deserialize để kiểm tra xem nó đã là một ApiResponse chưa
                    // Điều này giúp tránh bọc lại các phản hồi đã được bọc bởi ExceptionHandlingMiddleware
                    bool isAlreadyWrapped = false;
                    try
                    {
                        if (!string.IsNullOrEmpty(responseText)) // Chỉ parse nếu có nội dung
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
                        // Không phải JSON hợp lệ hoặc không theo định dạng ApiResponse của chúng ta, tiến hành bọc
                    }

                    if (!isAlreadyWrapped)
                    {
                        // Tạo một ApiResponse mới, bọc dữ liệu gốc vào trường Data
                        // Xử lý trường hợp responseText rỗng (ví dụ từ Ok(new {}))
                        JsonElement dataElement;
                        if (string.IsNullOrEmpty(responseText))
                        {
                            dataElement = JsonDocument.Parse("{}").RootElement; // Tạo một đối tượng JSON rỗng
                        }
                        else
                        {
                            dataElement = JsonDocument.Parse(responseText).RootElement;
                        }

                        var apiResponse = new ApiResponse<JsonElement>(
                            status: true,
                            code: "000",
                            message: "Success",
                            data: dataElement
                        );

                        // Tuần tự hóa ApiResponse đã bọc thành JSON
                        var wrappedJson = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                        // Đặt lại Content-Type và Content-Length trước khi ghi vào luồng gốc
                        context.Response.ContentType = "application/json";
                        context.Response.ContentLength = Encoding.UTF8.GetBytes(wrappedJson).Length; // Cập nhật Content-Length

                        await originalBodyStream.WriteAsync(Encoding.UTF8.GetBytes(wrappedJson)); // Ghi phản hồi đã bọc vào luồng gốc
                    }
                    else
                    {
                        // Nếu đã được bọc, chỉ ghi lại nội dung gốc
                        context.Response.ContentType = "application/json"; // Đảm bảo Content-Type vẫn là JSON
                        context.Response.ContentLength = Encoding.UTF8.GetBytes(responseText).Length; // Cập nhật Content-Length
                        await originalBodyStream.WriteAsync(Encoding.UTF8.GetBytes(responseText));
                    }
                }
                else
                {
                    // Đối với các phản hồi lỗi (không phải 2xx), hoặc phản hồi không phải JSON,
                    // chỉ ghi lại nội dung gốc. ExceptionHandlingMiddleware đã xử lý lỗi.
                    // Đảm bảo Content-Length được đặt đúng cho phản hồi gốc
                    context.Response.ContentLength = Encoding.UTF8.GetBytes(responseText).Length; // Cập nhật Content-Length
                    await originalBodyStream.WriteAsync(Encoding.UTF8.GetBytes(responseText));
                }
            }
        }
    }

    public static class ResponseWrappingMiddlewareExtensions
    {
        public static IApplicationBuilder UseResponseWrappingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ResponseWrappingMiddleware>();
        }
    }
}