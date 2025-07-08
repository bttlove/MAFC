// Dtos/ApiResponse.cs
using System.Text.Json;

namespace pviBase.Dtos // Đảm bảo namespace này khớp với project của bạn
{
    public class ApiResponse<T>
    {
        public bool Status { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }

        public ApiResponse() { }

        public ApiResponse(bool status, string code, string message, T? data)
        {
            Status = status;
            Code = code;
            Message = message;
            Data = data;
        }

        public ApiResponse(bool status, string code, string message)
        {
            Status = status;
            Code = code;
            Message = message;
            Data = default(T); // Gán giá trị mặc định cho Data
        }
    }

    public class ApiResponse
    {
        public bool Status { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }

        // Sửa constructor này để khởi tạo tất cả các thuộc tính non-nullable
        public ApiResponse(bool status, string code, string message)
        {
            Status = status;
            Code = code;
            Message = message;
        }
    }
}