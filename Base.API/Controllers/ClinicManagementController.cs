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
using static System.Net.WebRequestMethods;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ClincAdmin")]

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
        public async Task<IActionResult> CreateClinicUser([FromBody] ClinicUserDTO model)
        {

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                throw new BadRequestException("This email is already registered.");

            // 2. Transaction Setup (using statement ensures Dispose/Rollback on failure)
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {

                // 3. Mapping and Identity Creation
                //var user = MapAndCreateUser(model);
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
                //await AssignUserRoleAsync(user);
                var Role = model.UserType switch
                {
                    "ClincDoctor" => "ClincDoctor",
                    "ClincReceptionis" => "ClincReceptionis",
                    _ => throw new BadRequestException("Invalid user type specified."),
                };

                var roleResult = await _userManager.AddToRoleAsync(user, Role);
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
                    throw new NotFoundException("The specified clinic does not exist.");
                }

                if (model.UserType == "ClincDoctor")
                {
                    var ClincDoctorRepository = _unitOfWork.Repository<ClincDoctorProfile>();
                    var profile = model.ToClincDoctor();
                    await ClincDoctorRepository.AddAsync(profile);
                }
                else if (model.UserType == "ClincReceptionis")
                {
                    var ClincReceptionistRepository = _unitOfWork.Repository<ClincReceptionistProfile>();
                    var profile = model.ToClincReceptionist();
                    await ClincReceptionistRepository.AddAsync(profile);
                }
                // 6. Commit Transaction
                if (await _unitOfWork.CompleteAsync() > 0)
                {
                    await transaction.CommitAsync();
                    await _emailSender.SendEmailAsync(user.Email, "Registration Completed",
               $"<p>Your Password is: <b>{model.Password}</b>");
                    return Ok(new ApiResponseDTO(201, "Clinic user registered successfully.", null));
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
        public async Task<IActionResult> GetClinicDoctors()
        {
            var ClinicRepo = _unitOfWork.Repository<ClincDoctorProfile>();

            var spec = new BaseSpecification<ClincDoctorProfile>();
            var list = (await ClinicRepo.ListAsync(spec))
                .Select(e => new
                {
                    e.Id,
                    e.UserId,
                    e.User.FullName,
                    e.User.Email
                }).ToList();

            if (!list.Any())
            {
                throw new NotFoundException("No clinc requests are currently defined in the system.");
            }
            return Ok(new ApiResponseDTO(200, "All Doctors", list));
        }

        [HttpGet("clinic-users")]
        public async Task<IActionResult> GetClinicusers()
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
                .ToList().Select(e=> new
                {
                    e.Id,
                    e.FullName,
                    e.Email,
                    e.UserType,
                });

            if (!result.Any())
            {
                throw new NotFoundException("No clinc requests are currently defined in the system.");
            }
            return Ok(new ApiResponseDTO(200, "All Clinic Users", result));
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

    }

    public class ClinicUserDTO
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

        //public Clinic? Clinc { get; set; }

    }

    public static class ClinicUserExtensions
    {
        public static ApplicationUser ToUser(this ClinicUserDTO Dto)
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

        public static ClincDoctorProfile ToClincDoctor(this ClinicUserDTO Dto)
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
        public static ClincReceptionistProfile ToClincReceptionist(this ClinicUserDTO Dto)
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
    }
}