using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Interfaces
{
    // Assumption: IOtpService interface definition (used by AuthController)
    public interface IOtpService
    {
        Task<string> GenerateAndStoreOtpAsync(string userId, string email);
        Task<(bool isValid, string userId)> ValidateOtpAsync(string email, string otp);
        // 🛡️ إضافة وقائية: لحذف رمز OTP من المخزن بعد الاستخدام
        Task RemoveOtpAsync(string email);
    }
}
