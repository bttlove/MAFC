using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using pviBase.Dtos;
using pviBase.Helpers;

namespace pviBase.Services
{

    public class PviApiForwardService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "http://piastest.pvi.com.vn/API_CP/ManagerApplication/TaoDon_TNCN_ThongThuong";
        private readonly string _key = "1ab8972c95fe4e3e8bec7fe83a4cdaabnbb";

        public PviApiForwardService(HttpClient httpClient, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _httpClient = httpClient;
            _key = configuration["PviApi:Key"] ?? "";
        }

        /// <summary>
        /// Forward trực tiếp request thô (json, file) lên API PVI thật.
        /// </summary>
        /// <param name="json">Chuỗi json (dạng string, đã có đủ các trường, chưa có Sign)</param>
        /// <param name="file">File đính kèm (có thể null)</param>
        /// <returns>Response string từ API PVI</returns>
        public async Task<string> ForwardRawRequestToPviApi(string json, Microsoft.AspNetCore.Http.IFormFile file)
        {
            // Parse json thành object để kiểm tra
            var model = JsonSerializer.Deserialize<Human_ThongThuong_Content>(json);
            if (model == null)
                throw new Exception("Invalid JSON");

            // Không override Sign nếu đã có
            if (string.IsNullOrEmpty(model.Sign))
            {
                var signString = _key + model.ngay_batdau + model.thoihan_bh + model.ma_gdich_doitac + model.sotien_bh.ToString() + model.tong_phi_bh.ToString() + model.StartTime + model.EndTime;
                Console.WriteLine($"[PVI DEBUG] Tự tính Sign do chưa có sẵn: {signString}");
                model.Sign = Md5Helper.TinhMD5(signString);
            }
            else
            {
                Console.WriteLine($"[PVI DEBUG] Giữ nguyên Sign từ JSON đầu vào: {model.Sign}");
            }

            // Đảm bảo NguoiDinhKem không null
            if (model.NguoiDinhKem == null)
                model.NguoiDinhKem = new List<DanhSachDinhKem_ThongThuong>();

            // Đảm bảo FileAttach không null nếu không có file
            if (model.FileAttach == null && file == null)
                model.FileAttach = new List<File_Attach_Content>();

            // Nếu có file thì convert sang base64 và add vào FileAttach
            if (file != null)
            {
                using (var ms = new System.IO.MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();
                    var fileAttach = BuildFileAttach(file.FileName, fileBytes, Path.GetExtension(file.FileName), "GYC");
                    if (model.FileAttach == null) model.FileAttach = new List<File_Attach_Content>();
                    model.FileAttach.Add(fileAttach);
                }
            }

            var jsonBody = JsonSerializer.Serialize(model);
            Console.WriteLine($"[PVI DEBUG] JSON gửi sang PVI: {jsonBody}");
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[PVI DEBUG] Response từ PVI: {responseString}");
            return responseString;
        }


        public async Task<string> TaoDonThongThuongAsync(Human_ThongThuong_Content model)
        {
            // Tạo Sign
            var signString = _key + model.ngay_batdau + model.thoihan_bh + model.ma_gdich_doitac + model.sotien_bh.ToString() + model.tong_phi_bh.ToString() + model.StartTime + model.EndTime;
            model.Sign = Md5Helper.TinhMD5(signString);

            // Serialize model
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }

        // Helper chuyển file sang base64 và build File_Attach_Content
        public static File_Attach_Content BuildFileAttach(string fileName, byte[] fileBytes, string extension, string loaiTailieu)
        {
            return new File_Attach_Content
            {
                file_name = fileName,
                file_size = fileBytes.Length.ToString(),
                file_extension = extension,
                loai_tailieu = loaiTailieu,
                file_base64 = Convert.ToBase64String(fileBytes)
            };
        }
    }
}
