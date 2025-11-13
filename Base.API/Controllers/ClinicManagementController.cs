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

    public class ClincManagementController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ClincManagementController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [HttpPost("create-Clinc-user")]
        public async Task<IActionResult> CreateClincUser([FromBody] ClincUserDTO model)
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
                if (createUserResult is null)
                    throw new InternalServerException("An unexpected error occurred during user creation.");
                if (!createUserResult.Succeeded)
                    throw new BadRequestException(createUserResult.Errors.Select(e => e.Description));
                model.UserId = user.Id;
             

                //var Role = model.UserType switch
                //{
                //    "ClincDoctor" => "ClincDoctor",
                //    "ClincReceptionis" => "ClincReceptionis",
                //    _ => throw new BadRequestException("Invalid user type specified."),
                //};

                var roleResult = await _userManager.AddToRoleAsync(user, searchTypeEnum.ToString());
                if (!roleResult.Succeeded)
                {
                    // توحيد طريقة رمي الاستثناءات لتشمل رسائل الخطأ من Identity
                    var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                    throw new InternalServerException($"Failed to assign default role. Details: {errors}");
                }

                var cuurentuser = await _userManager.GetUserAsync(User);
                if (cuurentuser is null) throw new NotFoundException("Not Found user");

                var spec = new BaseSpecification<ClincAdminProfile>(c => c.UserId == cuurentuser.Id);
                var userrepository = _unitOfWork.Repository<ClincAdminProfile>();
                model.ClincId = (await userrepository.GetEntityWithSpecAsync(spec)).ClincId;
                if (string.IsNullOrEmpty(model.ClincId))
                {
                    throw new NotFoundException("The specified Clinc does not exist.");
                }
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
                    return Ok(new ApiResponseDTO(201, "Clinc user registered successfully.", null));
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

        [HttpGet("Doctors")]
        public async Task<IActionResult> GetClincDoctors()
        {
            var ClincRepo = _unitOfWork.Repository<ClincDoctorProfile>();

            var spec = new BaseSpecification<ClincDoctorProfile>();
            var list = (await ClincRepo.ListAsync(spec))
                .Select(e => new
                {
                    e.Id,
                    e.UserId,
                    e.User.FullName,
                    e.User.Email
                }).ToList();

            if (!list.Any())
            {
                throw new NotFoundException("No Clinc requests are currently defined in the system.");
            }
            return Ok(new ApiResponseDTO(200, "All Doctors", list));
        }

        [HttpGet("Clinc-users")]
        public async Task<IActionResult> GetClincusers()
        {
            var roles = new[] { "ClincDoctor", "ClincReceptionis" };
            var allUsers = new List<ApplicationUser>();

            foreach (var role in roles)
            {
                var roleUsers = await _userManager.GetUsersInRoleAsync(role);
                allUsers.AddRange(roleUsers);
            }

            var result = allUsers
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .ToList().Select(e => new
                {
                    e.Id,
                    e.FullName,
                    e.Email,
                    e.UserType,
                });

            if (!result.Any())
            {
                throw new NotFoundException("No Clinc requests are currently defined in the system.");
            }
            return Ok(new ApiResponseDTO(200, "All Clinc Users", result));
        }

        [HttpGet("user-types")]
        public async Task<IActionResult> GetUserTypes()
        {
            var Repo = _unitOfWork.Repository<UserType>();
            var spec = new BaseSpecification<UserType>();
            var list = (await Repo.ListAsync(spec)).Select(e => e.Name).ToHashSet<string>();
            if (!list.Any())
            {
                throw new NotFoundException("No User Types are currently defined in the system.");
            }
            return Ok(new ApiResponseDTO(200, "All User Types", list));
        }

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

            var cuurentuser = await _userManager.GetUserAsync(User);
            if (cuurentuser is null) throw new NotFoundException("Not Found user");

            var spec = new BaseSpecification<ClincAdminProfile>(c => c.UserId == cuurentuser.Id);
            var userrepository =  _unitOfWork.Repository<ClincAdminProfile>();
            model.ClincId = (await userrepository.GetEntityWithSpecAsync(spec)).ClincId;
            if (string.IsNullOrEmpty(model.ClincId))
            {
                throw new NotFoundException("The specified Clinc does not exist.");
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
                    throw new BadRequestException("Clinc Schedule data format is invalid.");
                }
                var repo = _unitOfWork.Repository<ClinicSchedule>();
                await repo.AddRangeAsync(Schedules);
                if (await _unitOfWork.CompleteAsync() > 0)
                {
                    await transaction.CommitAsync();
                    return Ok(new ApiResponseDTO(200, "Doctor Schedule added successfully."));
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
            var cuurentuser = await _userManager.GetUserAsync(User);
            if (cuurentuser is null) throw new NotFoundException("Not Found user");

            var userspec = new BaseSpecification<ClincAdminProfile>(c => c.UserId == cuurentuser.Id);
            var userrepository = _unitOfWork.Repository<ClincAdminProfile>();
            var ClincId = (await userrepository.GetEntityWithSpecAsync(userspec)).ClincId;
            if (string.IsNullOrEmpty(ClincId))
            {
                throw new NotFoundException("Not Available Clinc for Current User");
            }
            var Repo = _unitOfWork.Repository<ClinicSchedule>();
            var spec = new BaseSpecification<ClinicSchedule>(e=>e.ClinicId == ClincId);
            var list = (await Repo.ListAsync(spec)).Select(e => new { e.DoctorId, e.Day, e.StartTime, e.EndTime });
            if (!list.Any())
            {
                throw new NotFoundException("No Clinc Schedule are currently defined in the system.");
            }
            return Ok(new ApiResponseDTO(200, "All Clinc Schedule ", list));
        }

        [HttpGet("clinic-appointmentSlots")]
        public async Task<IActionResult> GetAppointmentSlots([FromBody] bool IsBooked)
        {
            var currentuser = await _userManager.GetUserAsync(User);
            if (currentuser is null) throw new NotFoundException("Not Found user");

            var userspec = new BaseSpecification<ClincAdminProfile>(c => c.UserId == currentuser.Id);
            var userrepository = _unitOfWork.Repository<ClincAdminProfile>();
            var ClincId = (await userrepository.GetEntityWithSpecAsync(userspec)).ClincId;
            if (string.IsNullOrEmpty(ClincId))
            {
                throw new NotFoundException("Not Available Clinc for Current User");
            }
            var Repo = _unitOfWork.Repository<AppointmentSlot>();
            var spec = new BaseSpecification<AppointmentSlot>(e=>e.IsBooked == IsBooked);
            var list = (await Repo.ListAsync(spec)).Select(e => new { e.Date, e.StartTime, e.EndTime });
            if (!list.Any())
            {
                throw new NotFoundException("No Appointments are currently defined in the system.");
            }
            return Ok(new ApiResponseDTO(200, "All Appointments", list));
        }

        [HttpGet("available-usertypes")]
        public async Task<IActionResult> GetAvailableUserTypesForCreateUsers()
        {
            var result = Enum.GetNames(typeof(AvailableUserTypesForCreateUsers)).ToList();
            if (!result.Any())
            {
                throw new NotFoundException("No User Types are currently defined in the system.");
            }
            return Ok(new ApiResponseDTO(200, "All UserTypes", result));
        }
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
        public  string? ClincId { get; set; }

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

    enum AvailableUserTypesForCreateUsers
    {
        ClinicDoctor,
        ClinicReceptionist,
        ClinicAdmin
    }
}