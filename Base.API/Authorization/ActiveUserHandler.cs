using Base.API.Controllers;
using Base.DAL.Models;
using Base.Repo.Interfaces;
using Base.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
namespace Base.API.Authorization
{
    public class ActiveUserHandler : AuthorizationHandler<ActiveUserRequirement>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IClinicServices _clinicServices;


        public ActiveUserHandler(UserManager<ApplicationUser> userManager, IClinicServices clinicServices)
        {
            _userManager = userManager;
            _clinicServices = clinicServices;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ActiveUserRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                context.Fail();
                return;
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || (user != null && !user.IsActive))
            {
                context.Fail();
                return;
            }
            if (Enum.TryParse<AvailableUserTypesForCreateUsers>(user.UserType, true, out AvailableUserTypesForCreateUsers searchTypeEnum))
            {
                var clinicId = searchTypeEnum switch
                {
                    AvailableUserTypesForCreateUsers.ClinicDoctor =>
                    user.ClincDoctorProfile?.ClincId,
                    AvailableUserTypesForCreateUsers.ClinicReceptionist =>
                    user.ClincReceptionistProfile?.ClincId,
                    AvailableUserTypesForCreateUsers.ClinicAdmin =>
                    user.ClincAdminProfile?.ClincId,
                };
                if (string.IsNullOrEmpty(clinicId))
                {
                    context.Fail();
                    return;
                }
                var clinic = await _clinicServices.GetClinicAsync(c => c.Id == clinicId && c.Status == ClinicStatus.active.ToString());
                if (clinic is null)
                {
                    context.Fail();
                    return;
                }
            }

            context.Succeed(requirement);
        }
    }
}

