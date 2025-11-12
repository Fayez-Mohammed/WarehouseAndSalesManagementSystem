using Azure.Core;
using Base.API.DTOs;
using Base.DAL.Models;
using Base.Repo.Interfaces;
using Base.Services.Implementations;
using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RepositoryProject.Specifications;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClincController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;

        public ClincController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        [HttpPost("Clinc-request")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateClinc([FromBody] ClincRegistrationDTO model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }
            var ClincRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(c => c.Email.ToLower() == model.Email.ToLower());
            var result = await ClincRepo.CountAsync(spec);
            if (result < 1)
            {
                var _Clinc = model.ToClinc();
                await ClincRepo.AddAsync(_Clinc);
                if (await _unitOfWork.CompleteAsync() > 0)
                    return Ok(new ApiResponseDTO(200, "We have received your request, and it is currently under review. An email will be sent after the review."));
            }
            throw new BadRequestException("An error occurred, please try again later.");
        }

        [HttpGet("Clincs-requests")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> GetClincsRequests()
        {
            //var ClincRepo = _unitOfWork.Repository<Clinc>();
            //var spec = new BaseSpecification<Clinc>(c => c.Status.ToLower() == "pending");
            //spec.AllIncludes.Add(c => c.Include(_c => _c.MedicalSpecialty));
            //var list = await ClincRepo.ListAsync(spec);
            //var result = list.ToClincDTOSet();
            var result = await GetClincsAsync(c => c.Status.ToLower() == ClincStatus.pending.ToString());
            if (!result.Any())
            {
                throw new NotFoundException("No Clinc requests are currently defined in the system.");
            }
            return Ok(new ApiResponseDTO(200, "All Requests", result));
        }

        [HttpPatch("approve-Clinc-request")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> ApproveClincsRequests([FromBody] string ClincId)
        {
            if (string.IsNullOrEmpty(ClincId)) throw new BadRequestException("ClincId is required");

            var ClincRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(c => c.Id == ClincId);
            spec.AllIncludes.Add(c => c.Include(_c => _c.MedicalSpecialty));
            var request = await ClincRepo.GetEntityWithSpecAsync(spec);

            if (request is null) throw new NotFoundException("ClincId not found");
            request.Status = "active";
            await ClincRepo.UpdateAsync(request);

            if (await _unitOfWork.CompleteAsync() > 0)
            {
                // ✅ تحقق لو الأدمن مش 
                var ClincadminUser = await _userManager.FindByEmailAsync(request.Email);
                if (ClincadminUser == null)
                {
                    ClincadminUser = new ApplicationUser
                    {
                        FullName = request.Name,
                        UserType = "ClincAdmin",
                        UserName = request.Email,
                        Email = request.Email,
                        EmailConfirmed = true,
                        ClincAdminProfile = new ClincAdminProfile()
                        {
                            ClincId = request.Id,
                        }
                    };

                    var password = GeneratePassword();

                    var result = await _userManager.CreateAsync(ClincadminUser, password);
                    if (result.Succeeded)
                    {

                        await _userManager.AddToRoleAsync(ClincadminUser, "ClincAdmin");
                        try
                        {
                            await _emailSender.SendEmailAsync(request.Email, "Clinc Add Request Acceptance",
                                ApproveClincsRequestsMail(request.Name, request.Email, password));
                        }
                        catch (Exception ex)
                        {
                            throw new BadRequestException("Failed to send mail");
                        }
                        return Ok(new ApiResponseDTO(200, "Clinc Now Available in System"));
                    }
                }
            }

            throw new BadRequestException("Failed to Approve Clinc Request");
        }

        [HttpGet("system-Clincs")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> GetSystemClincs()
        {
            //var ClincRepo = _unitOfWork.Repository<Clinc>();
            //var spec = new BaseSpecification<Clinc>(c => c.Status.ToLower() == "pending");
            //spec.AllIncludes.Add(c => c.Include(_c => _c.MedicalSpecialty));
            //var list = await ClincRepo.ListAsync(spec);
            //var result = list.ToClincDTOSet();
            var result = await GetClincsAsync(c => c.Status.ToLower() != ClincStatus.pending.ToString());
            if (!result.Any())
            {
                throw new NotFoundException("No Clincs are currently defined in the system.");
            }
            return Ok(new ApiResponseDTO(200, "All Requests", result));
        }

        [HttpPatch("activate-Clinc")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> ActivateClinc(string ClincId)
        {
            if (string.IsNullOrEmpty(ClincId)) throw new BadRequestException("ClincId is Required");
            var result = await ChangeClincStatusAsync(c => c.Id == ClincId, ClincStatus.active);
            if (result)
            {
                throw new NotFoundException("Faild To Activate Clinc");
            }
            return Ok(new ApiResponseDTO(200, "Clinc Activated"));
        }

        [HttpPatch("deactivate-Clinc")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> DeactivateClinc(string ClincId)
        {
            if (string.IsNullOrEmpty(ClincId)) throw new BadRequestException("ClincId is Required");
            var result = await ChangeClincStatusAsync(c => c.Id == ClincId, ClincStatus.notactive);
            if (result)
            {
                throw new NotFoundException("Faild To Deactivate Clinc");
            }
            return Ok(new ApiResponseDTO(200, "Clinc Deactivated"));
        }

        [HttpGet("Clinc-admins")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> GetClincAdmins(string ClincId)
        {
            var Repo = _unitOfWork.Repository<ClincAdminProfile>();
            var spec = new BaseSpecification<ClincAdminProfile>(e => e.ClincId == ClincId);
            var list = (await Repo.ListAsync(spec)).Select(e => new { e.User?.Id, e.User?.FullName }).ToHashSet();
            if (!list.Any())
            {
                throw new NotFoundException("No Admin are currently defined in this Clinc.");
            }
            return Ok(new ApiResponseDTO(200, "All Clinc Admins", list));
        }

        [HttpPost("Clincadmin-resetpassword")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> ResetPasswordforClincAdmin([FromBody] ClincAdminResetPasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }

            try
            {
                var user = await _userManager.FindByIdAsync(model.AdminId);
                if (user is null)
                {
                    throw new NotFoundException($"There is no user with UserId '{model.AdminId}'");
                }
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description);
                    throw new BadRequestException($"Reset Password failed: {string.Join(", ", errors)}");

                }
                try
                {
              
                    await _emailSender.SendEmailAsync(user.Email, "New Password for your Account",
                        GetClinicAdminAccountCreatedTemplate(user.FullName, "", user.Email, model.NewPassword, "", "", "", "", DateTime.Now.Year));
                }
                catch (Exception ex)
                {
                    throw new BadRequestException("Password Reseted Successfully but Failed to send mail");
                }
                // 3. Success response
                return Ok(new ApiResponseDTO(200, "Password Reset successfully."));
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                throw new InternalServerException("An unexpected error occurred during Reset Password");
            }
        }

        [HttpGet("create-Clinicadmin")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> CreateClinicAdmin([FromBody] ClincAdminProfileCreateDTO model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }
            var ClincadminUser = await _userManager.FindByEmailAsync(model.Email);
            if (ClincadminUser is not null) throw new BadRequestException("This email is exsits");


            ClincadminUser = new ApplicationUser
            {
                FullName = model.FullName,
                UserType = "ClincAdmin",
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                ClincAdminProfile = new ClincAdminProfile()
                {
                    ClincId = model.ClincId,
                }
            };

            var password = GeneratePassword();
            var result = await _userManager.CreateAsync(ClincadminUser, password);
            if (!result.Succeeded)
            {
                throw new BadRequestException("Faild to Create User");
            }
            await _userManager.AddToRoleAsync(ClincadminUser, "ClincAdmin");
            try
            {
                var clincrepo = _unitOfWork.Repository<Clinic>();
                var spec = new BaseSpecification<Clinic>(c => c.Id == model.ClincId);
                var clinic = await clincrepo.GetEntityWithSpecAsync(spec);
                await _emailSender.SendEmailAsync(model.Email, "your clinic admin account",
                    GetClinicAdminAccountCreatedTemplate(model.FullName, clinic.Name, model.Email, password, "", "", "", "", DateTime.Now.Year));
            }
            catch (Exception ex)
            {
                throw new BadRequestException("User Created Successfully but Failed to send mail");
            }
            return Ok(new ApiResponseDTO(200, $"'{model.FullName}' is Now Admin for ClinicId '{model.ClincId}'"));


        }



        #region Helper Method
        public static string GeneratePassword(int length = 12)
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()-_=+[]{}|;:,.<>?";

            string allChars = upper + lower + digits + special;
            StringBuilder password = new StringBuilder();
            RandomNumberGenerator rng = RandomNumberGenerator.Create();

            // Ensure password contains at least one of each required character type
            password.Append(GetRandomChar(upper, rng));
            password.Append(GetRandomChar(lower, rng));
            password.Append(GetRandomChar(digits, rng));
            password.Append(GetRandomChar(special, rng));

            // Fill remaining characters
            for (int i = password.Length; i < length; i++)
            {
                password.Append(GetRandomChar(allChars, rng));
            }

            // Shuffle the result for randomness
            return new string(password.ToString().OrderBy(_ => Guid.NewGuid()).ToArray());
        }

        private static char GetRandomChar(string charset, RandomNumberGenerator rng)
        {
            byte[] buffer = new byte[4];
            rng.GetBytes(buffer);
            int value = BitConverter.ToInt32(buffer, 0);
            return charset[Math.Abs(value) % charset.Length];
        }

        private static string ApproveClincsRequestsMail(string ClincName, string username, string password)
        {
            string systemName = "Clinc Management System";
            string loginUrl = "https://your-system-url.com/login";
            string supportEmail = "support@your-system.com";
            string supportPhone = "+20 100 000 0000";
            string passwordExpiryDate = DateTime.Now.AddDays(1).ToString("MMMM dd, yyyy");

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width,initial-scale=1' />
  <title>Clinc Registration Approved - {ClincName}</title>
  <style>
    body {{ font-family: Arial, sans-serif; direction: ltr; color: #222; }}
    .container {{ max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #eee; border-radius: 8px; background: #fff; }}
    .header {{ text-align: center; margin-bottom: 20px; }}
    .btn {{ display: inline-block; padding: 10px 18px; border-radius: 6px; text-decoration: none; font-weight: bold; }}
    .primary {{ background:#0b74de; color:#fff; }}
    .note {{ font-size: 13px; color: #555; margin-top: 12px; }}
    .creds {{ background:#f7f7f7; padding:12px; border-radius:6px; margin:12px 0; }}
    .footer {{ font-size:12px; color:#777; text-align:center; margin-top:18px; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h2>Welcome to {systemName}</h2>
      <p>Congratulations! The registration request for <strong>{ClincName}</strong> has been approved.</p>
    </div>

    <p>Here are your account details:</p>

    <div class='creds'>
      <p><strong>Username:</strong> {username}</p>
      <p><strong>Temporary Password:</strong> {password}</p>
    </div>

    <p>Click the button below to access the system:</p>
    <p style='text-align:center;'>
      <a href='{loginUrl}' class='btn primary'>Login to the System</a>
    </p>

    <p class='note'>
      <strong>Important Notes:</strong><br/>
      • Please change your password immediately after your first login from your account settings page.<br/>
      • The temporary password will expire on <strong>{passwordExpiryDate}</strong>.<br/>
      • For assistance, please contact us at <a href='mailto:{supportEmail}'>{supportEmail}</a> or call {supportPhone}.
    </p>

    <div class='footer'>
      Best regards,<br/>
      {systemName} Support Team
    </div>
  </div>
</body>
</html>";
        }

        private async Task<ICollection<ClincDTO>> GetClincsAsync(Expression<Func<Clinic, bool>> CriteriaExpression)
        {
            var ClincRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(CriteriaExpression);
            spec.AllIncludes.Add(c => c.Include(_c => _c.MedicalSpecialty));
            var list = await ClincRepo.ListAsync(spec);
            var result = list.ToClincDTOSet();
            return result;
        }

        private async Task<bool> ChangeClincStatusAsync(Expression<Func<Clinic, bool>> CriteriaExpression, ClincStatus status)
        {
            var ClincRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(CriteriaExpression);
            var Clinc = await ClincRepo.GetEntityWithSpecAsync(spec);
            Clinc.Status = status.ToString();
            await ClincRepo.UpdateAsync(Clinc);
            return (await _unitOfWork.CompleteAsync() > 0);
        }


        private static string GetClinicAdminAccountCreatedTemplate(
                string adminName,
                string clinicName,
                string adminEmail,
                string temporaryPassword,
                string activationLink,
                string supportEmail,
                string supportPhone,
                string organizationName,
                int year)
        {
            return $@"
<!doctype html>
<html lang='en'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<title>Clinic Admin Account Created</title>
<style>
  body,table,td,a{{-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;}}
  table{{border-collapse:collapse!important;}}
  img{{border:0;height:auto;line-height:100%;outline:none;text-decoration:none;}}
  body{{margin:0;padding:0;width:100%!important;font-family:Arial,Helvetica,sans-serif;background-color:#f4f6f8;color:#333;}}
  .email-wrapper{{width:100%;padding:20px 0;}}
  .email-content{{max-width:680px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 4px 18px rgba(15,15,15,0.08);}}
  .email-header{{padding:20px 28px;display:flex;align-items:center;gap:16px;}}
  .brand-logo{{width:48px;height:48px;border-radius:6px;background:#e9eef6;text-align:center;line-height:48px;font-weight:bold;color:#1a73e8;}}
  .brand-title{{font-size:18px;font-weight:700;color:#1f2937;}}
  .email-body{{padding:24px 28px;font-size:15px;line-height:1.5;color:#374151;}}
  .greeting{{font-size:16px;font-weight:600;margin-bottom:12px;}}
  .lead{{margin-bottom:18px;color:#4b5563;}}
  .card{{background:#f8fafc;border:1px solid #e6eef7;padding:16px;border-radius:8px;margin-bottom:18px;}}
  .credential-row{{display:flex;gap:16px;flex-wrap:wrap;}}
  .cred-item{{min-width:160px;background:#fff;border:1px solid #e5e7eb;padding:10px 12px;border-radius:6px;font-family:monospace;font-size:14px;color:#111827;}}
  .btn{{display:inline-block;padding:12px 20px;background:#2563eb;color:#fff;text-decoration:none;border-radius:8px;font-weight:600;margin-top:8px;}}
  .muted{{color:#6b7280;font-size:13px;margin-top:12px;}}
  .email-footer{{padding:18px 28px;font-size:13px;color:#6b7280;border-top:1px solid #f1f5f9;}}
  @media (max-width:520px){{.email-header,.email-body,.email-footer{{padding-left:16px;padding-right:16px;}}.credential-row{{flex-direction:column;}}}}
</style>
</head>
<body>
  <center class='email-wrapper'>
    <div class='email-content'>
      <div class='email-header'>
        <div class='brand-logo'>CL</div>
        <div>
          <div class='brand-title'>Clinic Management System</div>
          <div style='font-size:13px;color:#6b7280;'>Account notification</div>
        </div>
      </div>

      <div class='email-body'>
        <div class='greeting'>Hello {adminName},</div>

        <div class='lead'>
          An administrator account for <strong>{clinicName}</strong> has been created in the Clinic Management System.
        </div>

        <div class='card'>
          <div style='font-size:14px;font-weight:600;margin-bottom:8px;'>Your account details</div>

          <div class='credential-row'>
            <div class='cred-item'>
              <div style='font-size:12px;color:#6b7280;'>Email</div>
              <div>{adminEmail}</div>
            </div>

            <div class='cred-item'>
              <div style='font-size:12px;color:#6b7280;'>Temporary password</div>
              <div>{temporaryPassword}</div>
            </div>

            <div class='cred-item' style='min-width:220px;'>
              <div style='font-size:12px;color:#6b7280;'>Role</div>
              <div>Clinic Administrator</div>
            </div>
          </div>

          <div style='margin-top:12px;font-size:13px;color:#374151;'>
            For security, please activate your account and change the temporary password.
          </div>

          <div style='margin-top:16px;'>
            <a class='btn' href='{activationLink}' target='_blank' rel='noopener'>Activate your account</a>
          </div>

          <div class='muted'>
            If the button doesn't work, copy & paste the following URL into your browser:
            <div style='word-break:break-all;'>{activationLink}</div>
          </div>
        </div>

        <div style='font-size:14px;color:#374151;'>
          <strong>Next steps:</strong>
          <ul style='margin:8px 0 0 18px;color:#4b5563;'>
            <li>Click the activation link to set a new password and complete setup.</li>
            <li>After signing in, review clinic settings and staff permissions.</li>
            <li>If this wasn't you, contact support immediately.</li>
          </ul>
        </div>

        <div style='margin-top:18px;color:#374151;'>
          Thank you,<br>
          <strong>The Clinic Management Team</strong>
        </div>
      </div>

      <div class='email-footer'>
        Need help? Contact us at <a href='mailto:{supportEmail}'>{supportEmail}</a> or call {supportPhone}.<br>
        © {year} {organizationName}. All rights reserved.
      </div>
    </div>
  </center>
</body>
</html>";
        }


        #endregion
    }
    public class ClincRegistrationDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string MedicalSpecialtyId { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string? AddressCountry { get; set; }
        public string? AddressGovernRate { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressLocation { get; set; }
        public string? Phone { get; set; }
        //public string Status { get; set; } = "pending";
    }
    public class ClincDTO
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MedicalSpecialtyId { get; set; } = string.Empty;
        public string? MedicalSpecialtyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AddressCountry { get; set; }
        public string? AddressGovernRate { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressLocation { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; } = "pending";
    }
    public static class ClincExtensions
    {
        public static Clinic ToClinc(this ClincRegistrationDTO Dto)
        {
            if (Dto is null)
            {
                return new Clinic();
            }

            return new Clinic
            {
                Name = Dto.Name,
                MedicalSpecialtyId = Dto.MedicalSpecialtyId,
                Email = Dto.Email,
                AddressCountry = Dto.AddressCountry,
                AddressGovernRate = Dto.AddressGovernRate,
                AddressCity = Dto.AddressCity,
                AddressLocation = Dto.AddressLocation,
                Phone = Dto.Phone,
                Status = "pending",
            };
        }
        public static ClincDTO ToClincDTO(this Clinic entity)
        {
            if (entity is null)
            {
                return new ClincDTO();
            }

            return new ClincDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                MedicalSpecialtyId = entity.MedicalSpecialtyId,
                MedicalSpecialtyName = entity.MedicalSpecialty?.Name ?? "NA",
                Email = entity.Email,
                AddressCountry = entity.AddressCountry,
                AddressGovernRate = entity.AddressGovernRate,
                AddressCity = entity.AddressCity,
                AddressLocation = entity.AddressLocation,
                Phone = entity.Phone,
                Status = entity.Status,
            };
        }
        public static HashSet<ClincDTO> ToClincDTOSet(this IEnumerable<Clinic> entities)
        {
            if (entities == null)
                return new HashSet<ClincDTO>();

            return entities.Select(e => e.ToClincDTO()).ToHashSet();
        }

    }
    public class ClincAdminResetPasswordDTO
    {
        [Required]
        public required string AdminId { get; set; }
        [Required]
        public required string NewPassword { get; set; }
    }
    public class ClincAdminProfileCreateDTO
    {
        [Required]
        public required string ClincId { get; set; }
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string FullName { get; set; }
    }

    enum ClincStatus
    {
        pending,
        active,
        notactive
    }
}
