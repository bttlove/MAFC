using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pviBase.Models
{
    public class InsuranceContract
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string LoanNo { get; set; }

        [Required]
        [MaxLength(50)]
        public string LoanType { get; set; }

        [Required]
        public DateTime LoanDate { get; set; }

        [Required]
        [MaxLength(200)]
        public string CustName { get; set; }

        [Required]
        public DateTime CustBirthday { get; set; }

        [Required]
        [MaxLength(1)]
        public string CustGender { get; set; } // M or F

        [Required]
        [MaxLength(12)]
        public string CustIdNo { get; set; }

        [MaxLength(200)]
        public string CustAddress { get; set; }

        [Required]
        [MaxLength(10)]
        public string CustPhone { get; set; }

        [MaxLength(200)]
        public string CustEmail { get; set; }

        public long LoanAmount { get; set; } // VNĐ

        public int LoanTerm { get; set; } // months

        public double InsRate { get; set; }

        [Required]
        public DateTime DisbursementDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public byte[]? AttachmentData { get; set; }
        public string? AttachmentFileName { get; set; }
        public string? AttachmentContentType { get; set; }
    }
}
