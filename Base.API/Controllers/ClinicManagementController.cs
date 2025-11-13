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
using RepositoryProject.Specifications;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using static System.Net.WebRequestMethods;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ClincAdmin")]
    [Authorize(Policy = "ActiveUserOnly")]

    public class ClinicManagementController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ClinicManagementController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [HttpPost("create-clinic-user")]
        public async Task<IActionResult> CreateClinicUser([FromBody] ClincUserDTO model)
        {

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }

            AvailableUserTypesForCreateUsers searchTypeEnum;
            if (!Enum.TryParse<AvailableUserTypesForCreateUsers>(model.UserType, true, out searchTypeEnum))
                throw new InternalServerException("Invalid user type specified.");

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                throw new BadRequestException("This email is already registered.");

            // 2. Transaction Setup (using statement ensures Dispose/Rollback on failure)
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 3. Mapping and Identity Creation
                ApplicationUser user;
                try
                {
                    user = model.ToUser();
                }
                catch (Exception ex)
                {
                    throw new BadRequestException("Registration data format is invalid.");
                }

                // 4. Identity Creation - الآن نستخدم await بشكل صحيح
                var createUserResult = await _userManager.CreateAsync(user, model.Password);
                if (!createUserResult.Succeeded)
                    throw new BadRequestException(createUserResult.Errors.Select(e => e.Description));

                model.UserId = user.Id;
                var roleResult = await _userManager.AddToRoleAsync(user, searchTypeEnum.ToString());
                if (!roleResult.Succeeded)
                {
                    // توحيد طريقة رمي الاستثناءات لتشمل رسائل الخطأ من Identity
                    var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                    throw new InternalServerException($"Failed to assign default role. Details: {errors}");
                }

                var currentuser = await _userManager.GetUserAsync(User);
                if (currentuser is null) throw new NotFoundException("Not Found user");

                var spec = new BaseSpecification<ClincAdminProfile>(c => c.UserId == currentuser.Id);
                var userrepository = _unitOfWork.Repository<ClincAdminProfile>();
                model.ClincId = (await userrepository.GetEntityWithSpecAsync(spec)).ClincId;
                if (string.IsNullOrEmpty(model.ClincId))
                    throw new NotFoundException("The specified Clinc does not exist.");

                switch (searchTypeEnum)
                {
                    case AvailableUserTypesForCreateUsers.ClinicDoctor:
                        var ClinicDoctorRepository = _unitOfWork.Repository<ClincDoctorProfile>();
                        var Doctorprofile = model.ToClincDoctor();
                        await ClinicDoctorRepository.AddAsync(Doctorprofile);
                        break;
                    case AvailableUserTypesForCreateUsers.ClinicReceptionist:
                        var ClinicReceptionistRepository = _unitOfWork.Repository<ClincReceptionistProfile>();
                        var Receptionistprofile = model.ToClincReceptionist();
                        await ClinicReceptionistRepository.AddAsync(Receptionistprofile);
                        break;
                    case AvailableUserTypesForCreateUsers.ClinicAdmin:
                        var ClinicAdminRepository = _unitOfWork.Repository<ClincAdminProfile>();
                        var Adminprofile = model.ToClincAdminProfile();
                        await ClinicAdminRepository.AddAsync(Adminprofile);
                        break;
                }

                // 6. Commit Transaction
                if (await _unitOfWork.CompleteAsync() > 0)
                {
                    await transaction.CommitAsync();
                    await _emailSender.SendEmailAsync(user.Email, "Registration Completed",
               $"<p>Your Password is: <b>{model.Password}</b>");
                    return Ok(new { statusCode = 201, message = "Clinc user registered successfully." });
                }
                else
                {
                    await transaction.RollbackAsync();
                    throw new InternalServerException("Database transaction failed to save changes.");
                }
            }
            catch (Exception ex) when (ex is not BadRequestException)
            {
                await transaction.RollbackAsync();
                throw new InternalServerException("An unexpected error occurred during registration. Please try again.");
            }
        }

        [HttpGet("doctors")]
        public async Task<IActionResult> GetClinicDoctors()
        {
            var currentuser = await _userManager.GetUserAsync(User);
            if (currentuser is null) throw new NotFoundException("Not Found user");

            var currentspec = new BaseSpecification<ClincAdminProfile>(c => c.UserId == currentuser.Id);
            var userrepository = _unitOfWork.Repository<ClincAdminProfile>();
            var targetClinicId = (await userrepository.GetEntityWithSpecAsync(currentspec)).ClincId;

            var ClincRepo = _unitOfWork.Repository<ClincDoctorProfile>();
            var spec = new BaseSpecification<ClincDoctorProfile>(e => e.ClincId == targetClinicId);
            var list = (await ClincRepo.ListAsync(spec))
                .Select(e => new
                {
                    e.Id,
                    e.UserId,
                    e.User?.FullName,
                    e.User?.Email,
                    e.User?.IsActive
                }).ToList();

            if (!list.Any())
            {
                throw new NotFoundException("No Clinic requests are currently defined in the system.");
            }
            return Ok(new { message = "All Doctors", list });
        }

        [HttpGet("clinic-users")]
        public async Task<IActionResult> GetClinicUsers()
        {
            //var roles = new[] { "ClincDoctor", "ClincReceptionis" };
            //var allUsers = new List<ApplicationUser>();

            //foreach (var role in roles)
            //{
            //    var roleUsers = await _userManager.GetUsersInRoleAsync(role);
            //    allUsers.AddRange(roleUsers);
            //}

            //var result = allUsers
            //    .GroupBy(u => u.Id)
            //    .Select(g => g.First())
            //    .ToList().Select(e => new
            //    {
            //        e.Id,
            //        e.FullName,
            //        e.Email,
            //        e.UserType,
            //        e.IsActive
            //    });
            var currentuser = await _userManager.GetUserAsync(User);
            if (currentuser is null) throw new NotFoundException("Not Found user");

            var spec = new BaseSpecification<ClincAdminProfile>(c => c.UserId == currentuser.Id);
            var userrepository = _unitOfWork.Repository<ClincAdminProfile>();
            var targetClinicId = (await userrepository.GetEntityWithSpecAsync(spec)).ClincId;

            var validUserTypeNames = Enum.GetNames(typeof(AvailableUserTypesForCreateUsers));
            Expression<Func<ApplicationUser, bool>> expr = u =>
             validUserTypeNames.Contains(u.UserType) &&
             (
                 (u.ClincAdminProfile != null && u.ClincAdminProfile.ClincId == targetClinicId) ||
                 (u.ClincDoctorProfile != null && u.ClincDoctorProfile.ClincId == targetClinicId) ||
                 (u.ClincReceptionistProfile != null && u.ClincReceptionistProfile.ClincId == targetClinicId)
             );
            var result = _userManager.Users.Where(expr).Select(e => new
            {
                e.Id,
                e.FullName,
                e.Email,
                e.UserType,
                e.IsActive
            }).ToHashSet();

            if (!result.Any())
            {
                throw new NotFoundException("No Clinic requests are currently defined in the system.");
            }
            return Ok(new { message = "All Clinic Users", result });
        }

        //[HttpGet("user-types")]
        //public async Task<IActionResult> GetUserTypes()
        //{
        //    var Repo = _unitOfWork.Repository<UserType>();
        //    var spec = new BaseSpecification<UserType>();
        //    var list = (await Repo.ListAsync(spec)).Select(e => e.Name).ToHashSet<string>();
        //    if (!list.Any())
        //    {
        //        throw new NotFoundException("No User Types are currently defined in the system.");
        //    }
        //    return Ok(new ApiResponseDTO(200, "All User Types", list));
        //}

        [HttpPost("create-doctor-schedule")]
        public async Task<IActionResult> CreateDoctorSchedule([FromBody] DoctorScheduleDTO model)
        {

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }
            var existingUser = await _userManager.FindByIdAsync(model.DoctorId);
            if (existingUser != null)
                throw new BadRequestException("This doctor doesn't exist.");

            var currentuser = await _userManager.GetUserAsync(User);
            if (currentuser is null) throw new NotFoundException("Not Found user");

            var spec = new BaseSpecification<ClincAdminProfile>(c => c.UserId == currentuser.Id);
            var userrepository = _unitOfWork.Repository<ClincAdminProfile>();
            model.ClincId = (await userrepository.GetEntityWithSpecAsync(spec)).ClincId;
            if (string.IsNullOrEmpty(model.ClincId))
            {
                throw new NotFoundException("The specified Clinic does not exist.");
            }

            // 2. Transaction Setup (using statement ensures Dispose/Rollback on failure)
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                List<ClinicSchedule> Schedules;
                try
                {
                    Schedules = model.ToDoctorSchedule();
                }
                catch (Exception ex)
                {
                    throw new BadRequestException("Clinic Schedule data format is invalid.");
                }
                var repo = _unitOfWork.Repository<ClinicSchedule>();
                await repo.AddRangeAsync(Schedules);
                if (await _unitOfWork.CompleteAsync() > 0)
                {
                    await transaction.CommitAsync();
                    return Ok(new { message = "Doctor Schedule added successfully." });
                }
                else
                {
                    await transaction.RollbackAsync();
                    throw new InternalServerException("Database transaction failed to save changes.");
                }
            }
            catch (Exception ex) when (ex is not BadRequestException)
            {
                await transaction.RollbackAsync();
                throw new InternalServerException("An unexpected error occurred during registration. Please try again.");
            }
        }

        [HttpGet("clinic-schedule")]
        public async Task<IActionResult> GetClinicSchedule()
        {
            #region get current User ClincId

            var currentuser = await _userManager.GetUserAsync(User);
            if (currentuser is null) throw new NotFoundException("Not Found user");

            var userspec = new BaseSpecification<ClincAdminProfile>(c => c.UserId == currentuser.Id);
            var userrepository = _unitOfWork.Repository<ClincAdminProfile>();
            var ClincId = (await userrepository.GetEntityWithSpecAsync(userspec)).ClincId;
            if (string.IsNullOrEmpty(ClincId))
            {
                throw new NotFoundException("Not Available Clinic for Current User");
            }
            #endregion

            var Repo = _unitOfWork.Repository<ClinicSchedule>();
            var spec = new BaseSpecification<ClinicSchedule>(e => e.ClinicId == ClincId);
            spec.AllIncludes.Add(q => q.Include(c => c.Doctor).ThenInclude(u => u.User));
            var list = (await Repo.ListAsync(spec)).Select(e => new { e.Doctor.UserId, e.Doctor?.User?.FullName, Day = e.Day.ToString(), e.StartTime, e.EndTime });
            if (list.Count() == 0)
            {
                throw new NotFoundException("No Clinic Schedule are currently defined in the system.");
            }
            return Ok(new { message = "All Clinic Schedule ", list });
        }

        [HttpGet("clinic-appointmentSlots")]
        public async Task<IActionResult> GetAppointmentSlots([FromBody] bool IsBooked)
        {
            #region current user ClincId

            var currentuser = await _userManager.GetUserAsync(User);
            if (currentuser is null) throw new NotFoundException("Not Found user");

            var userspec = new BaseSpecification<ClincAdminProfile>(c => c.UserId == currentuser.Id);
            var userrepository = _unitOfWork.Repository<ClincAdminProfile>();
            var ClincId = (await userrepository.GetEntityWithSpecAsync(userspec)).ClincId;
            if (string.IsNullOrEmpty(ClincId))
            {
                throw new NotFoundException("Not Available Clinc for Current User");
            }
            #endregion

            var Repo = _unitOfWork.Repository<AppointmentSlot>();
            var spec = new BaseSpecification<AppointmentSlot>(e => e.IsBooked == IsBooked && e.DoctorSchedule.ClinicId == ClincId);
            spec.AllIncludes.Add(q => q.Include(c => c.DoctorSchedule).ThenInclude(s => s.Doctor).ThenInclude(s => s.User));
            var list = (await Repo.ListAsync(spec)).Select(e => new { e.DoctorSchedule?.Doctor?.User?.FullName, Day = e.DoctorSchedule?.Day.ToString(), e.Date, e.StartTime, e.EndTime });
            if (!list.Any())
            {
                throw new NotFoundException("No Appointments are currently defined in the system.");
            }
            return Ok(new { message = "All Appointments", list });
        }

        [HttpGet("available-usertypes")]
        public async Task<IActionResult> GetAvailableUserTypesForCreateUsers()
        {
            var result = Enum.GetNames(typeof(AvailableUserTypesForCreateUsers)).ToList();
            if (!result.Any())
            {
                throw new NotFoundException("No User Types are currently defined in the system.");
            }
            return Ok(new { message = "All UserTypes", result });
        }

        [HttpPost("clinicuser-resetpassword")]
        public async Task<IActionResult> ResetPasswordforClinicUser([FromBody] ClinicUserResetPasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }

            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user is null)
                {
                    throw new NotFoundException($"There is no user with UserId '{model.UserId}'");
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
                        ResetPasswordClinicUserTemplate(model.NewPassword));
                }
                catch (Exception ex)
                {
                    throw new BadRequestException("Password Reseted Successfully but Failed to send mail");
                }
                // 3. Success response
                return Ok(new { message = "Password Reset successfully." });
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                throw new InternalServerException("An unexpected error occurred during Reset Password");
            }
        }

        [HttpPost("activate-user")]
        public async Task<IActionResult> ActivateUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new BadRequestException("UserId is required");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            user.IsActive = true;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException("An unexpected error occurred during Change User Status. Please try again.");

            return Ok("User Activated successfully");
        }

        [HttpPost("deactivate-user")]
        public async Task<IActionResult> DeactivateUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new BadRequestException("UserId is required");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            user.IsActive = false;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException("An unexpected error occurred during Change User Status. Please try again.");

            return Ok("User Deactivated successfully");
        }

        [HttpPatch("clinic-setprice")]
        public async Task<IActionResult> SetClinicPrice([FromBody] double Price)
        {
            #region current user ClincId

            var currentuser = await _userManager.GetUserAsync(User);
            if (currentuser is null) throw new NotFoundException("Not Found user");

            var userspec = new BaseSpecification<ClincAdminProfile>(c => c.UserId == currentuser.Id);
            var userrepository = _unitOfWork.Repository<ClincAdminProfile>();
            var ClincId = (await userrepository.GetEntityWithSpecAsync(userspec)).ClincId;
            if (string.IsNullOrEmpty(ClincId))
            {
                throw new NotFoundException("Not Available Clinc for Current User");
            }
            #endregion

            var Repo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(e => e.Id == ClincId);
            var result = await Repo.GetEntityWithSpecAsync(spec);
            if (result is null)
            {
                throw new NotFoundException("No Clinic are currently defined in the system.");
            }
            result.Price = Price;
            await Repo.UpdateAsync(result);
            if (await _unitOfWork.CompleteAsync() <= 0)
            {
                throw new InternalServerException("An unexpected error occurred during Set Clinic Price. Please try again.");
            }
            return Ok(new { message = "Price set Successfully" });
        }

        [HttpGet("clinic-data")]
        public async Task<IActionResult> GetClinicData()
        {
            #region current user ClincId

            var currentuser = await _userManager.GetUserAsync(User);
            if (currentuser is null) throw new NotFoundException("Not Found user");

            var userspec = new BaseSpecification<ClincAdminProfile>(c => c.UserId == currentuser.Id);
            var userrepository = _unitOfWork.Repository<ClincAdminProfile>();
            var ClincId = (await userrepository.GetEntityWithSpecAsync(userspec)).ClincId;
            if (string.IsNullOrEmpty(ClincId))
            {
                throw new NotFoundException("Not Available Clinc for Current User");
            }
            #endregion
            var Repo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(e => e.Id == ClincId);
            spec.AllIncludes.Add(q => q.Include(c => c.MedicalSpecialty));
            var result = (await Repo.ListAsync(spec)).Select(e => new
            {
                e.Id,
                e.Name,
                e.Email,
                e.AddressCountry,
                e.AddressGovernRate,
                e.AddressCity,
                e.AddressLocation,
                e.Phone,
                e.Status,
                e.Price,
                MedicalSpecialty = new
                {
                    e.MedicalSpecialty?.Id,
                    e.MedicalSpecialty?.Name
                }
            });
            if (result is null)
            {
                throw new NotFoundException("No Clinic are currently defined in the system.");
            }
            return Ok(new { message = "Clinic Data", result });
        }


        #region Helper
        private static string ResetPasswordClinicUserTemplate(string NewPassword)
        {
            return $@"<!-- Email HTML template -->
<!doctype html>
<html lang=""en"">
  <head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width,initial-scale=1"" />
    <title>Account Info</title>
  </head>
  <body style=""margin:0;padding:0;background-color:#f4f4f5;font-family:Arial,Helvetica,sans-serif;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;margin:24px auto;background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 6px rgba(0,0,0,0.06);"">
      <tr>
        <td style=""padding:20px 24px;text-align:left;"">
          <h2 style=""margin:0 0 12px;font-size:20px;color:#111827;"">Hello, <span style=""font-weight:700"">{{{{FullName}}}}</span></h2>

          <p style=""margin:0 0 18px;font-size:15px;color:#374151;line-height:1.5;"">
            Your Password has been reseted. Below is your new password:
          </p>

          <div style=""display:inline-block;padding:12px 16px;border-radius:6px;background:#f3f4f6;border:1px solid #e5e7eb;margin-bottom:18px;"">
            <strong style=""font-size:16px;letter-spacing:0.2px;"">Password:</strong>
            <div style=""margin-top:6px;font-family: 'Courier New', Courier, monospace; font-size:16px;"">
              {NewPassword}
            </div>
          </div>

          <p style=""margin:0;font-size:13px;color:#6b7280;"">
            For security, please change this password after your first login.
          </p>
        </td>
      </tr>

      <tr>
        <td style=""padding:12px 24px;background:#fafafa;border-top:1px solid #f0f0f0;text-align:center;font-size:12px;color:#9ca3af;"">
          This is an automated message — please do not reply.
        </td>
      </tr>
    </table>

    <!-- Plain-text fallback for clients that strip HTML -->
    <div style=""display:none;white-space:nowrap;font:15px/1px monospace;color:#fff;max-height:0;overflow:hidden;"">
      Hello {{{{FullName}}}} — Your new password: {{{{NewPassword}}}} — Please change it after first login.
    </div>
  </body>
</html>

";
        }
        #endregion

    }
    #region ClincUser

    public class ClincUserDTO
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [PasswordPropertyText]
        public required string Password { get; set; }

        [Required]
        public required string FullName { get; set; }

        [Required]
        public required string UserType { get; set; }
        public string? UserId { get; set; }


        public string? ClincId { get; set; }

        //public Clinc? Clinc { get; set; }

    }

    public static class ClincUserExtensions
    {
        public static ApplicationUser ToUser(this ClincUserDTO Dto)
        {
            if (Dto is null)
            {
                return new ApplicationUser();
            }

            return new ApplicationUser
            {
                FullName = Dto.FullName,
                UserType = Dto.UserType,
                UserName = Dto.Email,
                Email = Dto.Email
            };
        }

        public static ClincDoctorProfile ToClincDoctor(this ClincUserDTO Dto)
        {
            if (Dto is null)
            {
                return new ClincDoctorProfile();
            }

            return new ClincDoctorProfile
            {
                UserId = Dto.UserId,
                ClincId = Dto.ClincId
            };
        }
        public static ClincReceptionistProfile ToClincReceptionist(this ClincUserDTO Dto)
        {
            if (Dto is null)
            {
                return new ClincReceptionistProfile();
            }

            return new ClincReceptionistProfile
            {
                UserId = Dto.UserId,
                ClincId = Dto.ClincId
            };
        }
        public static ClincAdminProfile ToClincAdminProfile(this ClincUserDTO Dto)
        {
            if (Dto is null)
            {
                return new ClincAdminProfile();
            }

            return new ClincAdminProfile
            {
                UserId = Dto.UserId,
                ClincId = Dto.ClincId
            };
        }
    }

    #endregion

    #region DoctorSchedule
    public class DoctorScheduleDTO
    {
        public string? ClincId { get; set; }

        [Required]
        public required string DoctorId { get; set; }

        [Required]
        public required int SlotDurationMinutes { get; set; }
        public ICollection<SlotsDTO> slots { get; set; } = new List<SlotsDTO>();

    }
    public class SlotsDTO
    {
        [Required]
        [EnumDataType(typeof(DayOfWeek), ErrorMessage = "Invalid day of the week.")]
        public DayOfWeek Day { get; set; }
        [Required]
        public TimeSpan StartTime { get; set; }
        [Required]
        public TimeSpan EndTime { get; set; }
    }
    public static class DoctorScheduleExtentions
    {
        public static List<ClinicSchedule> ToDoctorSchedule(this DoctorScheduleDTO Dto)
        {
            if (Dto is null || Dto.slots == null || !Dto.slots.Any())
            {
                return new List<ClinicSchedule>();
            }
            // لكل slot نعمل ClincSchedule جديد
            var schedules = Dto.slots.Select(slot => new ClinicSchedule
            {
                ClinicId = Dto.ClincId,
                DoctorId = Dto.DoctorId,
                SlotDurationMinutes = Dto.SlotDurationMinutes,
                Day = slot.Day,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime
            }).ToList();

            return schedules;
        }
    }
    #endregion
    public class ClinicUserResetPasswordDTO
    {
        [Required]
        public required string UserId { get; set; }
        [Required]
        public required string NewPassword { get; set; }
    }
    enum AvailableUserTypesForCreateUsers
    {
        ClinicDoctor,
        ClinicReceptionist,
        ClinicAdmin
    }
}