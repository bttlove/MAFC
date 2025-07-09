// Helpers/ErrorCodes.cs
// Định nghĩa các mã lỗi và thông báo lỗi dưới dạng hằng số để sử dụng nhất quán trong ứng dụng.
namespace pviBase.Helpers
{
    public static class ErrorCodes
    {
        // Mã thành công
        public const string SuccessCode = "000";
        public const string SuccessMessage = "Thành công";

        // Mã lỗi chung
        public const string AccessKeyNotFoundCode = "ERR_001";
        public const string AccessKeyNotFoundMessage = "Access key không tìm thấy.";

        public const string InvalidParametersCode = "ERR_002";
        public const string InvalidParametersMessage = "Các tham số không đúng định dạng.";

        public const string ContractNotFoundCode = "ERR_003";
        public const string ContractNotFoundMessage = "Không tìm thấy hợp đồng theo mã LoanNo được cung cấp.";

        public const string ContractPendingCode = "ERR_004";
        public const string ContractPendingMessage = "Hợp đồng đang được PVI Digital xử lý và chưa hoàn tất.";

        public const string ExceptionErrorsCode = "ERR_999";
        public const string ExceptionErrorsMessage = "Các lỗi ngoại lệ.";

        // Thông báo lỗi chung cho các lỗi không mong muốn
        public const string UnexpectedErrorMessage = "An unexpected error occurred. Please try again later.";
    }
}
