namespace pviBase.Dtos
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
            Data = default(T);
        }
    }

    public class ApiResponse
    {
        public bool Status { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }

     
        public object? Errors { get; set; }

        public ApiResponse(bool status, string code, string message)
        {
            Status = status;
            Code = code;
            Message = message;
        }

 
        public ApiResponse(bool status, string code, string message, object errors)
        {
            Status = status;
            Code = code;
            Message = message;
            Errors = errors;
        }
    }
}
