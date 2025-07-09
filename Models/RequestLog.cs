using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pviBase.Models
{
    public enum RequestStatus
    {
        Pending,    // Yêu cầu đã được nhận và đang chờ xử lý
        Processing, // Yêu cầu đang được xử lý
        Completed,  // Yêu cầu đã hoàn thành thành công
        Failed      // Yêu cầu đã thất bại
    }
    public class RequestLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } // Khóa chính tự tăng

        [Required]
        [MaxLength(50)]
        public string RequestId { get; set; } // ID duy nhất của yêu cầu (GUID hoặc ID từ client)

        [Required]
        [MaxLength(20)]
        public RequestStatus Status { get; set; } // Trạng thái hiện tại của yêu cầu

        [Required]
        public DateTime CreatedAt { get; set; } // Thời điểm yêu cầu được tạo

        public DateTime? LastUpdatedAt { get; set; } // Thời điểm trạng thái được cập nhật lần cuối

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; } // Thông báo lỗi nếu yêu cầu thất bại

        [Column(TypeName = "nvarchar(max)")] // Lưu trữ dữ liệu JSON của request
        public string? RequestData { get; set; } // Dữ liệu gốc của request (JSON)
    }
}
