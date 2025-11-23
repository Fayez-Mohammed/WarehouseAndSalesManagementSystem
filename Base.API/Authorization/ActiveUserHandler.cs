using Base.API.Controllers;
using Base.DAL.Models.BaseModels;
using Base.Repo.Interfaces;
using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Base.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
namespace Base.API.Authorization
{
    public class ActiveUserHandler : AuthorizationHandler<ActiveUserRequirement>
    {
        private readonly UserManager<ApplicationUser> _userManager;


        public ActiveUserHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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
            if (user == null)
            {
                context.Fail();
                return;
            }

            // 1️⃣ لو اليوزر غير نشط
            if (!user.IsActive)
            {
                // نضع علامة في HttpContext
                var httpContext = context.Resource as DefaultHttpContext;
                httpContext?.Items.Add("UserIsInactive", true);

                context.Fail();
                return;
            }
            var userType = user.Type;

            // لو كل شيء تمام
            context.Succeed(requirement);
        }
    }
}

