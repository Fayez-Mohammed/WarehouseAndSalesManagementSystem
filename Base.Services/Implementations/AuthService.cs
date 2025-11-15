using Base.DAL.Models;
using Base.Repo.Implementations;
using Base.Repo.Interfaces;
using Base.Services.Helpers;
using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RepositoryProject.Specifications;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Base.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly IOtpService _otpService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _config;
        private const string DefaultRole = "User";
        private const string DefaultUserType = "User";

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            IOtpService otpService,
            IUnitOfWork unitOfWork,
            ILogger<AuthService> logger,
            IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _otpService = otpService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Logins the asynchronous.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">model</exception>
        public async Task<LoginResult> LoginUserAsync(LoginDTO model)
        {
            // يجب أن يتم التحقق من صحة ModelState في الـ Controller، لكن التحقق من الـ null مهم هنا
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            // 1. جلب المستخدم والتحقق من كلمة المرور
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                await Task.Delay(500); // Protective: تأخير زمني
                return new LoginResult { Success = false, Message = "Invalid credentials." };
            }

            // 2. التحقق من تأكيد الإيميل
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                try
                {
                    SendOtpAsync(user.Email);
                }
                catch (Exception ex)
                {
                    // Logging the email failure here
                    _logger?.LogError(ex, "Failed to resend confirmation email to {Email}", user.Email);
                }
                return new LoginResult
                {
                    Success = true,
                    RequiresOtpVerification = user.TwoFactorEnabled,
                    EmailConfirmed = user.EmailConfirmed,
                    Message = "Your account is not confirmed. A new confirmation email has been sent."
                };
            }

            // 3. التحقق من قفل الحساب
            if (await _userManager.IsLockedOutAsync(user))
            {
                return new LoginResult { Success = false, Message = "Your account is locked. Please try again later." };
            }


            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                // 4.1. المصادقة الثنائية مفعلة - توليد OTP وإرساله
                try
                {
                    await SendOtpAsync(user.Email);
                    return new LoginResult
                    {
                        Success = true,
                        RequiresOtpVerification = user.TwoFactorEnabled,
                        EmailConfirmed = user.EmailConfirmed,
                        Message = "Credentials accepted. A One-Time Password (OTP) has been sent to your email."
                    };
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to send OTP email during login for user {UserId}", user.Id);
                    return new LoginResult
                    {
                        Success = false,
                        Message = "Failed to send OTP email. Please try again later."
                    };
                }
            }
            else
            {
                // 4.2. المصادقة الثنائية غير مفعلة - إصدار JWT
                var token = await GenerateJwtToken(user);
                if (token == null)
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "Failed to generate authentication token. Please try again later."
                    };
                }
                var roles = await _userManager.GetRolesAsync(user);
                return new LoginResult
                {
                    Success = true,
                    Message = "Login successful.",
                    RequiresOtpVerification = user.TwoFactorEnabled,
                    EmailConfirmed = user.EmailConfirmed,
                    Token = token,
                    user = new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.UserType,
                        Roles = roles
                    }

                };
            }
        }


        /// <summary>
        /// Verifies the login asynchronous.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">model</exception>
        /// <exception cref="System.InvalidOperationException">User authentication failed (User ID in OTP store not found).</exception>
        public async Task<LoginResult> VerifyLoginAsync(VerifyOtpDTO model)
        {
            try
            {
                if (model is null) throw new ArgumentNullException(nameof(model));

                var (isValid, userId) = await _otpService.ValidateOtpAsync(model.Email, model.Otp);
                if (!isValid) return new LoginResult { Success = false, Message = "Invalid OTP. Please try again later." };

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    throw new InvalidOperationException("User authentication failed (User ID in OTP store not found).");

                try { await _otpService.RemoveOtpAsync(model.Email); } catch { /* best-effort */ }

                var token = await GenerateJwtToken(user);
                if (token == null)
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "Failed to generate authentication token. Please try again later."
                    };
                }

                var roles = await _userManager.GetRolesAsync(user);

                return new LoginResult
                {
                    Success = true,
                    Message = "Login successful.",
                    RequiresOtpVerification = user.TwoFactorEnabled,
                    EmailConfirmed = user.EmailConfirmed,
                    Token = token,
                    user = new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.UserType,
                        Roles = roles

                    }
                };
            }
            catch (Exception)
            {
                try { await _otpService.RemoveOtpAsync(model.Email); } catch { /* best-effort */ }
                throw;
            }
        }

        /// <summary>Sends the otp asynchronous.</summary>
        /// <param name="email">The email.</param>
        /// <exception cref="RepositoryProject.Services.BadRequestException">Invalid request data.
        /// or
        /// This email is Not registered.</exception>
        public async Task SendOtpAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new BadRequestException("Invalid request data.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) throw new BadRequestException("This email is Not registered.");
            try
            {
                var otp = await _otpService.GenerateAndStoreOtpAsync(user.Id, user.Email!);
                await _emailSender.SendEmailAsync(user.Email, "Your OTP Code",
                    $"<p>Your OTP verification code is: <b>{otp}</b></p><p>It will expire in 5 minutes.</p>");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send OTP");
                throw new BadRequestException("Failed to send OTP");
            }
        }


        /// <summary>
        /// Returns true when verification succeeded; false when OTP invalid/expired.
        /// Throws on other failures (e.g. confirm-email failed).
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">model</exception>
        /// <exception cref="System.InvalidOperationException">User authentication failed (User ID in OTP store not found).</exception>
        /// <exception cref="Base.Services.Implementations.BadRequestException">Email confirmation failed: {string.Join(", ", errors)}</exception>
        public async Task<bool> VerifyEmailAsync(VerifyOtpDTO model)
        {
            try
            {
                if (model is null) throw new ArgumentNullException(nameof(model));

                var (isValid, userId) = await _otpService.ValidateOtpAsync(model.Email, model.Otp);
                if (!isValid) return false;

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    throw new InvalidOperationException("User authentication failed (User ID in OTP store not found).");

                if (!user.EmailConfirmed)
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var result = await _userManager.ConfirmEmailAsync(user, token);
                    if (!result.Succeeded)
                    {
                        var errors = result.Errors.Select(e => e.Description).ToList();
                        throw new BadRequestException($"Email confirmation failed: {string.Join(", ", errors)}");
                    }
                }
                try { await _otpService.RemoveOtpAsync(model.Email); } catch { /* best-effort */ }
                return true;

            }
            catch (Exception)
            {
                try { await _otpService.RemoveOtpAsync(model.Email); } catch { /* best-effort */ }
                return false;
                throw;
            }
        }

        public async Task<string> VerifyForgetPassword(VerifyForgetPasswordDTO model)
        {
            try
            {
                if (model is null) throw new ArgumentNullException(nameof(model));

                var (isValid, userId) = await _otpService.ValidateOtpAsync(model.Email, model.Otp);
                if (!isValid) return "Invalid OTP";

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    throw new InvalidOperationException($"Security flow error: User Email '{model.Email}' associated with valid OTP was not found.");
                
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                try { await _otpService.RemoveOtpAsync(model.Email); } catch { /* best-effort */ }
                return token;

            }
            catch (Exception ex)
            {
                try { await _otpService.RemoveOtpAsync(model.Email); } catch { /* best-effort */ }
                return ex.Message;
                throw;
            }
        }

        /// <summary>
        /// Resets the password.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>
        ///   <see langword="if" /> true if reset succeeded; otherwise, false (invalid/expired OTP).
        /// </returns>
        /// <exception cref="System.ArgumentNullException">model</exception>
        /// <exception cref="System.InvalidOperationException">Security flow error: User Email '{model.Email}' associated with valid OTP was not found.</exception>
        /// <exception cref="Base.Services.Implementations.BadRequestException">Password reset failed: {string.Join(", ", errors)}</exception>
        public async Task<bool> ResetPassword(ResetPasswordDTO model)
        {
            try
            {

                if (model is null) throw new ArgumentNullException(nameof(model));

                /*var (isValid, userId) = await _otpService.ValidateOtpAsync(model.Email, model.Otp);

                if (!isValid) return false;*/

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                    throw new BadRequestException("Invalid request");

                var isValid = await _userManager.VerifyUserTokenAsync(user,_userManager.Options.Tokens.PasswordResetTokenProvider,
                    "ResetPassword",model.Token);
                if (!isValid) return false;

                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    throw new BadRequestException($"Password reset failed: {string.Join(", ", errors)}");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
                throw;

            }
        }


        /// <summary>
        /// Changes the password asynchronous.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="model">The model.</param>
        /// <exception cref="System.ArgumentNullException">model</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">User with ID '{userId}' was not found.</exception>
        /// <exception cref="Base.Services.Implementations.BadRequestException">Change Password failed: {string.Join(", ", errors)}</exception>
        public async Task ChangePasswordAsync(string userId, ChangePasswordDTO model)
        {
            try
            {
                if (model is null) throw new ArgumentNullException(nameof(model));

                // 1. User Validation (Moved from controller, but checks the ID passed by controller)
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) throw new NotFoundException($"User with ID '{userId}' was not found.");

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description);
                    throw new BadRequestException($"Change Password failed: {string.Join(", ", errors)}");

                }

            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                throw new InternalServerException("An unexpected internal error occurred during Change Password.");
            }

        }

        /// <summary>
        /// Generates the JWT token.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>
        ///   <see langword="The" /> JWT token as string.
        /// </returns>
        public async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            // 1. Protective: Check for vital JWT configurations
            var jwtKey = _config["Jwt:Key"];
            var jwtIssuer = _config["Jwt:Issuer"];
            var jwtAudience = _config["Jwt:Audience"];

            // Check key length (min 32 chars for 256-bit security)
            if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32 || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                // Log the error internally
                _logger?.LogError("JWT configuration settings are missing or key is too short (min 32 characters required).");
                return null;
            }

            try
            {
                // 2. Build Core Claims
                var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };

                // Get and add Roles
                var roles = await _userManager.GetRolesAsync(user);
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // 3. Generate Key and Credentials
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Use UtcNow for better time zone handling
                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                // Log any unexpected failure during token generation
                _logger?.LogError(ex, "An unexpected error occurred during JWT token generation for user {UserId}.", user.Id);
                return null;
            }
        }

        /// <summary>
        /// Handel External Login
        /// </summary>
        /// <param name="email"></param>
        /// <param name="fullName"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        /// <exception cref="InternalServerException"></exception>
        public async Task<ExternalLoginResponseDTO> HandleExternalLoginAsync(string email, string fullName)
        {
            if (email is null) throw new BadRequestException("Not Valid Email.");
            var profileRepository = _unitOfWork.Repository<UserProfile>();
            IDbContextTransaction transaction = null;

            // 1. البحث عن المستخدم
            var user = await _userManager.FindByEmailAsync(email);
            bool newUser = (user == null);
            UserProfile profile = null;

            try
            {
                // ----------------------------------------------------------------------
                // 2. معالجة المستخدم الجديد (ضمن معاملة متكاملة)
                // ----------------------------------------------------------------------
                if (newUser)
                {
                    // 🟢 وقائي: بدء معاملة
                    transaction = await _unitOfWork.BeginTransactionAsync();

                    user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                    var createResult = await _userManager.CreateAsync(user, Guid.NewGuid().ToString("N"));
                    if (createResult is null) throw new BadRequestException("Failed to create new user account.");
                    if (!createResult.Succeeded)
                    {
                        if (transaction != null) await transaction.RollbackAsync();
                        // استخدام استثناء ليرسله الـ Controller كـ 500
                        throw new BadRequestException("Failed to create new user account.");
                    }
                    await _userManager.AddToRoleAsync(user, "User");

                    // 💡 إنشاء Profile للمستخدم الجديد
                    profile = new UserProfile
                    {
                        UserId = user.Id,
                        FullName = fullName ?? email,
                        PhoneNumber = ""
                    };

                    await profileRepository.AddAsync(profile);

                    await _unitOfWork.CompleteAsync();
                    await transaction.CommitAsync();
                }

                // ----------------------------------------------------------------------
                // 3. معالجة المستخدم الحالي (أو الجديد بعد الإنشاء)
                // ----------------------------------------------------------------------

                // 💡 إذا كان المستخدم قديمًا، يتم جلب ملف التعريف هنا
                if (!newUser)
                {
                    // نفترض أن GetByIdAsync يجلب البروفايل بناءً على user.Id
                    profile = await profileRepository.GetByIdAsync(user.Id);
                }

                // 💡 معالجة Profile المفقود أو تحديث الاسم
                if (profile == null)
                {
                    profile = new UserProfile
                    {
                        UserId = user.Id,
                        FullName = fullName ?? email,
                        PhoneNumber = ""
                    };

                    await profileRepository.AddAsync(profile);
                    await _unitOfWork.CompleteAsync();
                }
                else if (string.IsNullOrEmpty(profile.FullName) && !string.IsNullOrEmpty(fullName))
                {
                    // Protective: تحديث الاسم إذا كان موجودًا في Claim لكنه مفقود من Profile
                    profile.FullName = fullName;
                    await profileRepository.UpdateAsync(profile);
                    await _unitOfWork.CompleteAsync();
                }

                // ----------------------------------------------------------------------
                // 4. توليد JWT وإرجاع البيانات
                // ----------------------------------------------------------------------

                var token = await GenerateJwtToken(user);

                if (token == null)
                {
                    throw new BadRequestException("Failed to generate JWT token after successful login/creation.");
                }

                var roles = await _userManager.GetRolesAsync(user);
                return new ExternalLoginResponseDTO
                {
                    Token = token,
                    user = new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        Roles = roles
                    }
                };
            }
            catch (Exception ex)
            {
                // 🟢 Protective: التراجع عن المعاملة في حالة حدوث أي خطأ غير متوقع
                if (transaction != null)
                {
                    // Log the error (ex) here
                    await transaction.RollbackAsync();
                }
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                throw new InternalServerException("An unexpected internal error occurred during HandleExternalLogin.");
            }
        }


        #region Registertion refactor
        public async Task RegisterAsync(RegisterDTO model)
        {
            // 1. Input Validation
            if (model is null)
                throw new ArgumentNullException(nameof(model));
            var ClincRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(c => c.Email.ToLower() == model.Email.ToLower());
            var result = (await ClincRepo.CountAsync(spec)) > 0 || (await _userManager.FindByEmailAsync(model.Email) is not null);
            if (result) throw new BadRequestException("This email is already registered.");

            // 2. Transaction Setup (using statement ensures Dispose/Rollback on failure)
            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 3. Mapping and Identity Creation
                var user =await MapAndCreateUser(model);

                // 4. Identity Creation - الآن نستخدم await بشكل صحيح
                var createUserResult = await _userManager.CreateAsync(user, model.Password);

                if (createUserResult is null)
                    throw new InternalServerException("An unexpected error occurred during user creation.");

                if (!createUserResult.Succeeded)
                {
                    // رمي BadRequestException إذا فشل Identity Framework في الإنشاء
                    throw new BadRequestException(createUserResult.Errors.Select(e => e.Description));
                }

                // 4. Role Assignment (Handling role creation if necessary is better done during app startup/seeding)
                await AssignUserRoleAsync(user);

                // 5. Profile Creation (Data layer logic)
                await CreateUserProfileAsync(user.Id, model);

                // 6. Commit Transaction
                if (await _unitOfWork.CompleteAsync() > 0)
                {
                    await transaction.CommitAsync();
                }
                else
                {
                    // If CompleteAsync returns 0 but didn't throw, something is wrong, force Rollback
                    await transaction.RollbackAsync();
                    throw new InternalServerException("Database transaction failed to save changes.");
                }

                // 7. Post-Registration Action (Best-effort, non-critical)
                // Note: The original code threw BadRequestException on failed OTP send, 
                // which might break the user experience unnecessarily. We revert to a simple log and continue.
                await SendRegistrationOtpIfPossible(user);
            }
            catch (BadRequestException ex)
            {
                // Caught BadRequest from Identity or Mapping errors. Rollback and re-throw.
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex) when (ex is not BadRequestException)
            {
                // Catch all other exceptions (DB failure, unexpected Identity errors, etc.)
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Unexpected critical error during user registration for {Email}", model.Email);

                // Wrap unexpected exceptions in InternalServerException
                throw new InternalServerException("An unexpected error occurred during registration. Please try again.");
            }
        }
        #region 
        /// <summary>
        /// Maps the DTO to ApplicationUser and creates the user in Identity system.
        /// </summary>
        private async Task<ApplicationUser> MapAndCreateUser(RegisterDTO model)
        {
            try
            {
                // Mapping logic separated for cleanliness
                var user = model.ToUser();
                user.UserType = DefaultUserType;
                return user;

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to map RegisterDTO for {Email}", model.Email);
                throw new BadRequestException("Registration data format is invalid.");
            }
        }

        /// <summary>
        /// Ensures the default role exists and assigns it to the user.
        /// Note: Role creation should ideally be done during application seeding.
        /// </summary>
        private async Task AssignUserRoleAsync(ApplicationUser user)
        {
            // ملاحظة: يُفضل نقل هذه الدالة المساعدة إلى مكان يُنفذ مرة واحدة عند بدء التشغيل (Seeding).
            await EnsureDefaultRoleExistsAsync(DefaultRole);

            // 1. تعيين الدور للمستخدم
            var roleResult = await _userManager.AddToRoleAsync(user, DefaultRole);

            if (!roleResult.Succeeded)
            {
                // توحيد طريقة رمي الاستثناءات لتشمل رسائل الخطأ من Identity
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign default role '{Role}' to user {UserId}: {Errors}", DefaultRole, user.Id, errors);

                throw new InternalServerException($"Failed to assign default role. Details: {errors}");
            }
        }

        /// <summary>
        /// تضمن وجود الدور الافتراضي، وتقوم بإنشائه إذا لم يكن موجوداً.
        /// يُفضل استدعاء هذه الدالة مرة واحدة أثناء تهيئة التطبيق (Application Startup Seeding).
        /// </summary>
        /// <param name="roleName">اسم الدور.</param>
        private async Task EnsureDefaultRoleExistsAsync(string roleName)
        {
            //if (await _roleManager.Roles.AnyAsync(r => r.Name == roleName))
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole(roleName);
                var createRoleResult = await _roleManager.CreateAsync(role);
                if (createRoleResult is null) throw new InternalServerException($"CRITICAL: Failed to create default role '{roleName}'.");
                if (!createRoleResult.Succeeded)
                {
                    var errors = string.Join("; ", createRoleResult.Errors.Select(e => e.Description));
                    _logger.LogCritical("CRITICAL: Failed to create essential default role '{Role}'. Details: {Errors}", roleName, errors);

                    // يجب رمي استثناء داخلي يمنع استمرار العملية إذا فشل إنشاء دور أساسي
                    throw new InternalServerException($"CRITICAL: Failed to create default role '{roleName}'. Details: {errors}");
                }
            }
        }

        /// <summary>
        /// Creates and saves the UserProfile entity linked to the new user.
        /// </summary>
        private async Task CreateUserProfileAsync(string userId, RegisterDTO model)
        {
            var profileRepository = _unitOfWork.Repository<UserProfile>();
            var profile = model.ToProfile();
            profile.UserId = userId;

            // We don't check profileResult success here, as _unitOfWork.CompleteAsync() will reveal DB errors.
            await profileRepository.AddAsync(profile);
        }

        /// <summary>
        /// Tries to send an OTP email without blocking the registration process if it fails.
        /// </summary>
        private async Task SendRegistrationOtpIfPossible(ApplicationUser user)
        {
            try
            {
                if (user.Email is not null)
                    await SendOtpAsync(user.Email);
            }
            catch (Exception ex)
            {
                // 💡 Key Change: Log the failure and continue. 
                // The original code threw BadRequestException here, which breaks SRP and the atomic commit concept.
                _logger.LogWarning(ex, "Failed to send OTP after successful registration for {Email}", user.Email);
            }
        }
        #endregion
        #endregion
    }
}