using System.Collections.Generic;

namespace pviBase.Dtos
{
    public class Human_ThongThuong_Content
    {
        public string nguoi_thuhuong { get; set; }
        public string dia_chi_th { get; set; }
        public string quyen_loibh { get; set; }
        public bool dtbh_tg_cho_01 { get; set; }
        public bool dtbh_tg_cho_02 { get; set; }
        public bool dtbh_tg_cho_03 { get; set; }
        public string CpId { get; set; }
        public string Sign { get; set; }
        public string EndTime { get; set; }
        public string StartTime { get; set; }
        public string dien_thoai { get; set; }
        public string khach_hang { get; set; }
        public long sotien_bh { get; set; }
        public string thoihan_bh { get; set; }
        public double phi_tyle_phi { get; set; }
        public double tong_phi_bh { get; set; }
        public string Email { get; set; }
        public string ngay_batdau { get; set; }
        public string dia_chi { get; set; }
        public string ma_gdich_doitac { get; set; }
        public string ma_sp { get; set; }
        public string ma_chuongtrinh { get; set; }
        public List<DanhSachDinhKem_ThongThuong> NguoiDinhKem { get; set; }
        public string sohopdong_tindung { get; set; }
        public string ngayhopdong_tindung { get; set; }
        public double laisuat_chovay { get; set; }
        public List<File_Attach_Content> FileAttach { get; set; } // Thêm property này để gửi file base64
    }

    public class DanhSachDinhKem_ThongThuong
    {
        public string ho_ten { get; set; }
        public string gioi_tinh { get; set; }
        public string ngay_sinh { get; set; }
        public string dia_chi { get; set; }
        public string dien_thoai { get; set; }
        public string cmt_hc { get; set; }
    }

    public class File_Attach_Content
    {
        public string file_name { get; set; }
        public string file_size { get; set; }
        public string file_extension { get; set; }
        public string loai_tailieu { get; set; }
        public string file_base64 { get; set; } // Thêm property này để gửi file base64
    }
}
