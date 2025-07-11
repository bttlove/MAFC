using System.Security.Cryptography;
using System.Text;

namespace pviBase.Helpers
{
    public static class Md5Helper
    {
        public static string TinhMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower(); // lowercase để trùng với Postman
            }
        }
    }
}
