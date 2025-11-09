using Base.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Implementations
{
    // نموذج البيانات الذي سنقوم بتخزينه في الذاكرة المؤقتة لكل OTP
    public class OtpEntry
    {
        public string UserId { get; set; }
        public string Code { get; set; }
    }

    // تنفيذ واجهة IOtpService باستخدام IMemoryCache
    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<OtpService> _logger;
        private static readonly Random Rng = new Random();
        // ثابت يحدد طول كود OTP (6 أرقام)
        private const int OtpLength = 6;
        // 💡 ثابت وقائي: يُستخدم كمفتاح أساسي (Prefix) في الـ Cache لضمان عدم التضارب
        private const string OtpCacheKeyPrefix = "OTP_";
        private readonly IConfiguration _configuration;

        public OtpService(IMemoryCache cache, ILogger<OtpService> logger, IConfiguration configuration)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// توليد كود OTP عشوائي وتخزينه في الذاكرة المؤقتة بوقت انتهاء محدد.
        /// المفتاح هو البريد الإلكتروني لسهولة الاسترجاع.
        /// </summary>
        /// <param name="userId">معرف المستخدم</param>
        /// <param name="email">بريد المستخدم (يستخدم كمفتاح التخزين)</param>
        /// <param name="lifetime">فترة صلاحية الكود</param>
        /// <returns>كود OTP الذي تم توليده</returns>
        public Task<string> GenerateAndStoreOtpAsync(string userId, string email)
        {
            var expiration = _configuration.GetValue<int>("OtpSettings:ExpirationMinutes");
            var lifetime = TimeSpan.FromMinutes(expiration);
            // توليد OTP (6 أرقام عشوائية)
            string otpCode = Rng.Next((int)Math.Pow(10, OtpLength - 1), (int)Math.Pow(10, OtpLength)).ToString();

            var entry = new OtpEntry { UserId = userId, Code = otpCode };

            // تخزين الـ OTP في الذاكرة المؤقتة
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(lifetime); // تعيين وقت الانتهاء

            // المفتاح هو البريد الإلكتروني
            _cache.Set(email.ToLowerInvariant(), entry, cacheEntryOptions);

            _logger.LogInformation("Generated OTP for {Email} with expiry: {ExpiryTime} minutes.",
                email, lifetime.TotalMinutes);

            return Task.FromResult(otpCode);
        }

        /// <summary>
        /// التحقق من صحة كود OTP المدخل ومطابقته مع المخزن.
        /// </summary>
        /// <param name="email">بريد المستخدم (مفتاح التخزين)</param>
        /// <param name="otp">الكود المدخل من المستخدم</param>
        /// <returns>زوج يوضح ما إذا كان الكود صحيحاً ومعرف المستخدم المرتبط به</returns>
        public Task<(bool isValid, string userId)> ValidateOtpAsync(string email, string otp)
        {
            // استخدام TryGetValue لضمان التعامل مع الحالات التي ينتهي فيها صلاحية الكود
            if (_cache.TryGetValue(email.ToLowerInvariant(), out OtpEntry storedEntry))
            {
                // المقارنة بين الكود المدخل والكود المخزن
                if (storedEntry.Code == otp)
                {
                    // Protective: إزالة الكود مباشرة بعد الاستخدام الناجح لمنع إعادة الاستخدام
                    _cache.Remove(email.ToLowerInvariant());
                    _logger.LogInformation("OTP successfully validated and removed for {Email}.", email);
                    return Task.FromResult((true, storedEntry.UserId));
                }
            }

            _logger.LogWarning("OTP validation failed for {Email}. Either code is invalid or expired.", email);

            // في حالة عدم العثور على المفتاح أو عدم تطابق الكود
            return Task.FromResult((false, string.Empty));
        }

        /// <summary>
        /// يحذف رمز OTP من الذاكرة المؤقتة (Cache) لضمان عدم إعادة استخدامه.
        /// </summary>
        /// <param name="email">البريد الإلكتروني الذي تم استخدامه كمفتاح للتخزين.</param>
        public Task RemoveOtpAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                // 🛡️ وقائي: منع الفشل إذا كان الإيميل فارغًا
                return Task.CompletedTask;
            }

            var cacheKey = $"{OtpCacheKeyPrefix}{email}";

            // 🟢 الإجراء الوقائي: حذف المفتاح من الذاكرة المؤقتة
            _cache.Remove(cacheKey);

            // 💡 بما أن عملية الإزالة من IMemoryCache متزامنة (Synchronous)، نرجع Task مكتمل
            return Task.CompletedTask;
        }
    }
}
