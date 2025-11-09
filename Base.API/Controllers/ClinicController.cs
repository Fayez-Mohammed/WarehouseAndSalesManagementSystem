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
using RepositoryProject.Specifications;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClinicController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;

        public ClinicController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        [HttpPost("clinc-request")]
        public async Task<IActionResult> CreateClinic([FromBody] ClinicRegistrationDTO model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }
            var ClinicRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(c => c.Email.ToLower() == model.Email.ToLower());
            var result = await ClinicRepo.CountAsync(spec);
            if (result < 1)
            {
                var _clinc = model.ToClinic();
                await ClinicRepo.AddAsync(_clinc);
                if (await _unitOfWork.CompleteAsync() > 0)
                    return Ok(new ApiResponseDTO(200, "We have received your request, and it is currently under review. An email will be sent after the review."));
            }
            throw new BadRequestException("An error occurred, please try again later.");
        }

        [HttpGet("clinics-requests")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> GetClinicsRequests()
        {
            var ClinicRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(c => c.Status.ToLower() == "pending");
            spec.AllIncludes.Add(c => c.Include(_c => _c.MedicalSpecialty));
            var list = await ClinicRepo.ListAsync(spec);
            var result = list.ToClinicDTOSet();
            if (!list.Any())
            {
                throw new NotFoundException("No clinc requests are currently defined in the system.");
            }
            return Ok(new ApiResponseDTO(200, "All Requests", result));
        }

        [HttpPatch("approve-clinic-request")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> ApproveClinicsRequests([FromBody] string clincId)
        {
            if (string.IsNullOrEmpty(clincId)) throw new BadRequestException("clincId is required");

            var ClinicRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(c => c.Id == clincId);
            spec.AllIncludes.Add(c => c.Include(_c => _c.MedicalSpecialty));
            var request = await ClinicRepo.GetEntityWithSpecAsync(spec);

            if (request is null) throw new NotFoundException("clincId not found");
            request.Status = "active";

            // ✅ تحقق لو الأدمن مش 
            var clincadminUser = await _userManager.FindByEmailAsync(request.Email);
            if (clincadminUser == null)
            {
                clincadminUser = new ApplicationUser
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

                var result = await _userManager.CreateAsync(clincadminUser, password);
                if (result.Succeeded)
                {

                    await _userManager.AddToRoleAsync(clincadminUser, "ClincAdmin");
                    try
                    {
                        await _emailSender.SendEmailAsync(request.Email, "clinc Add Request Acceptance",
                            ApproveClinicsRequestsMail(request.Name, request.Email, password));
                    }
                    catch (Exception ex)
                    {
                        throw new BadRequestException("Failed to send mail");
                    }
                    return Ok(new ApiResponseDTO(200, "Clinc Now Available in System"));
                }
            }
            throw new BadRequestException("Failed to Approve Clinc Request");
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

        private static string ApproveClinicsRequestsMail(string clinicName, string username, string password)
        {
            string systemName = "Clinic Management System";
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
  <title>Clinic Registration Approved - {clinicName}</title>
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
      <p>Congratulations! The registration request for <strong>{clinicName}</strong> has been approved.</p>
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

        #endregion
    }
    public class ClinicRegistrationDTO
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
    public class ClinicDTO
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
    public static class ClinicExtensions
    {
        public static Clinic ToClinic(this ClinicRegistrationDTO Dto)
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
        public static ClinicDTO ToClinicDTO(this Clinic entity)
        {
            if (entity is null)
            {
                return new ClinicDTO();
            }

            return new ClinicDTO
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
        public static HashSet<ClinicDTO> ToClinicDTOSet(this IEnumerable<Clinic> entities)
        {
            if (entities == null)
                return new HashSet<ClinicDTO>();

            return entities.Select(e => e.ToClinicDTO()).ToHashSet();
        }

    }
}
