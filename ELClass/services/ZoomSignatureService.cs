using System.Security.Cryptography;
using System.Text;

namespace ELClass.services
{
    public class ZoomSignatureService
    {
        private readonly IConfiguration _config;

        public ZoomSignatureService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// توليد signature للـ Zoom Web SDK
        /// role: 0 = مشارك (طالب) | 1 = مضيف (مدرس)
        /// </summary>
        public string GenerateSignature(string meetingNumber, int role = 0)
        {
            var sdkKey = _config["ZoomSettings:ClientId"];
            var sdkSecret = _config["ZoomSettings:ClientSecret"];

            // 1. التوقيت: لازم يكون Unix Timestamp بالثواني
            // ملحوظة: نقص 60 ثانية لضمان أن سيرفر زوم لا يرفض الطلب بسبب فرق التوقيت
            long iat = DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeSeconds();
            long exp = iat + 60 * 60 * 2; // صالح لمدة ساعتين

            // 2. الترتيب الصارم جداً (Strict Order)
            // لاحظ: لا يوجد أي فواصل أو نقاط هنا، فقط دمج النصوص
            string message = $"{sdkKey}{meetingNumber}{iat}{role}{exp}";

            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sdkSecret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                string hashBase64 = Convert.ToBase64String(hash);

                // 3. تجميع الأجزاء بالنقاط (هنا الترتيب حيوي)
                string rawSignature = $"{sdkKey}.{meetingNumber}.{iat}.{role}.{exp}.{hashBase64}";

                // 4. تحويل السلسلة كاملة لـ Base64 (تأكد من استخدام UTF8)
                byte[] rawSignatureBytes = Encoding.UTF8.GetBytes(rawSignature);
                return Convert.ToBase64String(rawSignatureBytes);
            }
        }
    }
}