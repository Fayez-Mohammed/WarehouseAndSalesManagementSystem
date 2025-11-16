using Base.API.DTOs;
using Base.Services.Implementations;
using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Base.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Logins the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        /// <exception cref="Base.Services.Implementations.BadRequestException">
        /// An unexpected error occurred during the final login step.
        /// or
        /// An unexpected internal error occurred during login.
        /// </exception>
        [HttpPost("login")]
        //// 🟢 الحالة الناجحة (تم إصدار التوكن)
        //[ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
        //// ⚠️ الحالات التي تتطلب إجراء إضافي (OTP أو تأكيد إيميل)
        //[ProducesResponseType(typeof(LoginResult), StatusCodes.Status202Accepted)]
        //// 🛑 بيانات اعتماد غير صالحة
        //[ProducesResponseType(typeof(LoginResult), StatusCodes.Status401Unauthorized)]
        //// 🚫 الحساب مقفل
        //[ProducesResponseType(typeof(LoginResult), StatusCodes.Status403Forbidden)]
        //// 💥 أخطاء داخلية (فشل إرسال OTP/توليد توكن)
        //[ProducesResponseType(typeof(LoginResult), StatusCodes.Status500InternalServerError)]
        //[ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)] // 👈 يحدد شكل الاستجابة
        //[ProducesResponseType((int)HttpStatusCode.BadRequest)]
        //[ProducesResponseType((int)HttpStatusCode.NotFound)]
        //[ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        //[ProducesResponseType((int)HttpStatusCode.Forbidden)]
        //[ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            // 1. Model State Validation 
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }
            try
            {
                // 2. Delegate to Service Layer
                var result = await _authService.LoginUserAsync(model);

                // 3. Translate Result to HTTP Response

                if (!result.Success)
                {
                    // حالة الفشل (بيانات خاطئة، إيميل غير مؤكد، حساب مقفل)
                    throw new UnauthorizedException(result.Message); // 401 Unauthorized
                }
                if (!result.EmailConfirmed)
                {
                    // نجاح جزئي: يتطلب التحقق من OTP
                    return Ok(new { statusCode = 201, result });
                }

                if (result.RequiresOtpVerification)
                {
                    // نجاح جزئي: يتطلب التحقق من OTP
                    return Ok(result);
                }

                // نجاح تام (تم إرجاع التوكن مباشرة لأن 2FA غير مفعل)
                if (!string.IsNullOrEmpty(result.Token) && result.user is not null)
                {
                    return Ok(result);
                }

                // حالة غير متوقعة (فشل توليد التوكن داخل الخدمة)
                throw new BadRequestException("An unexpected error occurred during the final login step.");
            }
            catch (BadRequestException ex)
            {
                // يلتقط الاستثناءات الناتجة عن أخطاء في البيانات (مثل أن النموذج يكون null)
                throw new BadRequestException(ex.Message);
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                // Log the exception
                throw new InternalServerException("An unexpected internal error occurred during login.");
            }
        }

        /*[HttpPost("refresh-token")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest model)
        {
            var result = await _authService.RefreshAsync(model);
            return Ok(result);
        }*/

        /// <summary>
        /// Verifies the login.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        /// <exception cref="Base.Services.Implementations.BadRequestException">
        /// An unexpected error occurred during OTP verification.
        /// </exception>
        /// 
        /// <remarks>
        /// هذا الـ endpoint يعرض جميع المستخدمين الموجودين في قاعدة البيانات.
        /// يمكن فلترة النتائج حسب الحاجة.
        /// </remarks>
        /// <response code="200">Token and User info</response>
        /// <response code="400">Invalid OTP. Please try again later.</response>
        /// <response code="401">User authentication failed (User ID not found).</response>
        /// <response code="403">Forbidden to access this end point</response>
        /// <response code="404">Not Found Any User</response>
        /// <response code="500">An unexpected error occurred during OTP verification.</response>
        [HttpPost("verify-login")]
        public async Task<IActionResult> VerifyLogin([FromBody] VerifyOtpDTO model)
        {
            // 1. Model State Validation
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }

            // 2. Delegate to Service Layer
            try
            {
                var result = await _authService.VerifyLoginAsync(model);

                if (!result.Success)
                {
                    // فشل التحقق من OTP (401)
                    throw new UnauthorizedException(result.Message);
                }

                // نجاح تام بعد التحقق من OTP
                // بما أن result.Success = true, يجب أن يكون result.Data != null
                return Ok(new { statusCode = 200, message = "Token and User info", result }); // 200 OK (Data contains Token and User info)

            }
            catch (InvalidOperationException ex)
            {
                // إذا لم يتم العثور على المستخدم بعد التحقق من OTP (خطأ داخلي)
                throw new NotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;

                // لأي خطأ غير متوقع
                throw new InternalServerException("An unexpected error occurred during OTP verification.");
            }
        }


        /// <summary>
        /// Registers a new user with the provided registration details.
        /// </summary>
        /// <remarks>This method requires a valid <see cref="RegisterModel"/> object to be passed in the
        /// request body.  Ensure that all required fields are correctly filled to avoid validation errors.</remarks>
        /// <param name="model">The registration details of the user, including necessary information such as username, password, and email.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the registration operation. Returns an HTTP 200 OK
        /// response with a success message if registration is successful.</returns>
        /// <exception cref="BadRequestException">Thrown if the provided registration details are invalid, containing a list of validation error messages.</exception>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    throw new BadRequestException(errors);
                }

                await _authService.RegisterAsync(model);
                return Ok(new { statusCode = 200, message = "The user has successfully registered. Please check your email to confirm your account." });

            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                throw new InternalServerException("An unexpected internal error occurred during Register.");

            }
        }


        /// <summary>
        /// Sends a One-Time Password (OTP) to the specified email address.
        /// </summary>
        /// <remarks>This method initiates the process of sending an OTP to the user's email address. The
        /// caller should ensure that the email address provided is valid and accessible by the user. Upon successful
        /// execution, the user should proceed to verify the OTP using the appropriate endpoint.</remarks>
        /// <param name="Email">The email address to which the OTP will be sent. Cannot be null, empty, or whitespace.</param>
        /// <returns>An <see cref="IActionResult"/> indicating that the request has been accepted and the OTP has been sent.</returns>
        /// <exception cref="BadRequestException">Thrown if <paramref name="Email"/> is null, empty, or consists only of whitespace.</exception>
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] string Email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Email))
                    throw new BadRequestException("Email is Required.");

                await _authService.SendOtpAsync(Email);
                return Ok(new { statusCode = 200, message = "A One-Time (OTP) has been sent to your email." });


            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                throw new InternalServerException("An unexpected internal error occurred during Send Otp.");
            }
        }


        /// <summary>Verifies the email.</summary>
        /// <param name="model">The model.</param>
        /// <returns>
        /// </returns>
        /// <exception cref="RepositoryProject.Services.BadRequestException"></exception>
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyOtpDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    throw new BadRequestException(errors);
                }

                var ok = await _authService.VerifyEmailAsync(model);
                if (!ok) throw new UnauthorizedException("Invalid or expired OTP code.");
                return Ok(new { statusCode = 200, message = "Your email address has been successfully confirmed. You can now log in." });
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                throw new InternalServerException("An unexpected internal error occurred during Verify Email.");
            }
        }

        /// <summary>
        /// Verifies the forget password.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        /// <exception cref="BadRequestException">errors</exception>
        /// <exception cref="UnauthorizedException">Invalid or expired OTP code.</exception>
        /// <exception cref="InternalServerException">An unexpected internal error occurred during Verify ForgetPassword.</exception>
        [HttpPost("verify-forgetpassword")]
        public async Task<IActionResult> VerifyForgetPassword([FromBody] VerifyForgetPasswordDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    throw new BadRequestException(errors);
                }

                var result = await _authService.VerifyForgetPassword(model);
                if (string.IsNullOrEmpty(result)) throw new UnauthorizedException("Invalid or expired OTP code.");
                return Ok(new { Message = "Your Token.", Token = result });
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                throw new InternalServerException("An unexpected internal error occurred during Verify ForgetPassword.");
            }
        }


        /// <summary>
        /// Forgots the password.
        /// </summary>
        /// <param name="Email">The email.</param>
        /// <returns></returns>
        /// <exception cref="Base.Services.Implementations.BadRequestException">Invalid request data.</exception>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] string Email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Email))
                    throw new BadRequestException("Invalid request data.");

                await _authService.SendOtpAsync(Email);
                return Ok(new { statusCode = 200, message = "A One-Time (OTP) has been sent to your email." });

            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                throw new InternalServerException("An unexpected internal error occurred during Forgot Password.");
            }
        }


        /// <summary>
        /// Resets the password.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        /// <exception cref="Base.Services.Implementations.BadRequestException">
        /// Reset Password failed.
        /// </exception>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    throw new BadRequestException(errors);
                }

                var ok = await _authService.ResetPassword(model);
                if (!ok) throw new BadRequestException("Reset Password failed.");

                return Ok(new { statusCode = 200, message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                throw new InternalServerException("An unexpected internal error occurred during Reset Password.");
            }
        }


        /// <summary>
        /// Changes the password.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        /// <exception cref="Base.Services.Implementations.BadRequestException">
        /// The current user could not be located.
        /// or
        /// An unexpected error occurred.
        /// </exception>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            // 1. Get UserId from the authenticated token claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                // This is a defense-in-depth check. If [Authorize] is used, 
                // this usually means the token is invalid or corrupted.
                throw new UnauthorizedException("Authentication failed: User ID claim missing or invalid.");
            }
            try
            {
                // 2. Delegate business logic to the service layer
                await _authService.ChangePasswordAsync(userId, model);
                // 3. Success response
                return Ok(new { statusCode = 200, message = "Password changed successfully." });
            }
            // 4. Handle specific business exceptions from the service
            catch (BadRequestException ex)
            {
                // Catches errors like "Current password is wrong" or "New password does not meet policy"
                throw new InternalServerException(ex.Message);
            }
            catch (NotFoundException)
            {
                // Catches if the user ID from the token somehow doesn't exist in the database
                throw new NotFoundException("The current user could not be located.");
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                // Catch any unexpected exceptions and log them internally
                // _logger.LogError(ex, "Unexpected error changing password for user {UserId}", userId);
                throw new InternalServerException("An unexpected error occurred during Change Password");
            }
        }

        #region External login        

        #region Google
        /// <summary>
        /// Googles the login.
        /// </summary>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            // Protective: التأكد من أن RedirectUri يشير إلى مسار داخل تطبيقك
            var redirectUrl = Url.Action(nameof(GoogleResponse), "Auth");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Googles the response.
        /// </summary>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            // 1. التحقق من المصادقة الخارجية
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (result?.Principal == null || !result.Succeeded)
                throw new BadRequestException("External authentication failed.");

            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var fullName = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

            // 🟢 مهم: تسجيل خروج المستخدم من الكوكيز الخارجية بعد جلب البيانات
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            if (string.IsNullOrEmpty(email))
                throw new NotFoundException("External provider did not return an email address.");

            try
            {
                // 2. تفويض كل منطق العمل إلى طبقة الخدمة
                var loginResponse = await _authService.HandleExternalLoginAsync(email, fullName);

                // 3. إرجاع النتيجة (Token and User data)
                return Ok(new { statusCode = 200, message = "Token and User data", loginResponse });
            }
            catch (BadRequestException ex)
            {
                // التقاط أخطاء مثل 'External provider did not return an email' إذا حدثت في الخدمة
                throw new BadRequestException(ex.Message);
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                // Log the error (ex) here
                throw new InternalServerException("An unexpected error occurred during Google sign-in process.");
            }
        }
        #endregion

        #region Facebook        
        /// <summary>
        /// Facebooks the login.
        /// </summary>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("facebook-login")]
        public IActionResult FacebookLogin()
        {
            // استخدام FacebookDefaults.AuthenticationScheme
            var redirectUrl = Url.Action(nameof(FacebookResponse), "Auth");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }
        /// <summary>
        /// Facebooks the response.
        /// </summary>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("facebook-response")]
        public async Task<IActionResult> FacebookResponse()
        {
            // 1. التحقق من المصادقة الخارجية
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (result?.Principal == null || !result.Succeeded)
                throw new BadRequestException("External authentication failed.");

            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var fullName = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

            // 🟢 مهم: تسجيل خروج المستخدم من الكوكيز الخارجية بعد جلب البيانات
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            if (string.IsNullOrEmpty(email))
                // تغيير الرسالة لتكون خاصة بالمنفذ الخارجي المستخدم حالياً
                throw new NotFoundException("External provider (Facebook) did not return an email address.");

            try
            {
                // 2. تفويض كل منطق العمل إلى نفس دالة الخدمة المشتركة
                var loginResponse = await _authService.HandleExternalLoginAsync(email, fullName);

                // 3. إرجاع النتيجة (Token and User data)
                return Ok(new { statusCode = 200, message = "Token and User data", loginResponse });

            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                // Log the error (ex) here
                throw new InternalServerException("An unexpected error occurred during Facebook sign-in process.");
            }
        }
        #endregion

        #endregion


        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest model)
        {
            // read ip & userAgent if not supplied
            var ip = model.Ip ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = model.UserAgent ?? Request.Headers["User-Agent"].ToString();

            try
            {
                var response = await _authService.RefreshAsync(model with { Ip = ip, UserAgent = ua });
                // Option: set refresh token as an HttpOnly Secure cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(int.Parse(/* from config */ "30"))
                };
                Response.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions);

                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }
    }
}
