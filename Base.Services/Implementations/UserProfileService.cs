using Base.DAL.Models;
using Base.Repo.Interfaces;
using Base.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RepositoryProject.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Implementations
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserProfileService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // 💡 الدالة التي تنفذ حذف البروفايل والمستخدم معًا
        public async Task<bool> DeleteProfileAndUserAsync(string userID)
        {            
            var profileRepository = _unitOfWork.Repository<UserProfile>();

            // 1️⃣ جلب البروفايل بدون تتبع (عشان ما يحصلش conflict)
            var spec = new BaseSpecification<UserProfile>(e => e.UserId.ToLower() == userID.ToLower());
            var profile = await profileRepository.GetEntityWithSpecAsync(spec);
            if (profile == null)
                return false;

            // 2️⃣ حذف البروفايل
            await profileRepository.DeleteAsync(profile);
            var saved = await _unitOfWork.CompleteAsync() > 0;

            //    _logger.LogWarning("لم يتم حفظ التغييرات بعد حذف البروفايل {ProfileId}", profileId);

            if (saved)
            {

                IdentityResult userDeleteResult = IdentityResult.Success;

                // 3️⃣ حذف المستخدم المرتبط
                if (!string.IsNullOrEmpty(profile.UserId))
                {
                    var userToDelete = await _userManager.FindByIdAsync(profile.UserId);
                    if (userToDelete != null)
                    {
                        try
                        {
                            userDeleteResult = await _userManager.DeleteAsync(userToDelete);
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            // 🛑 ده هو السيناريو اللي بيظهر فيه الخطأ Optimistic concurrency failure
                            //_logger.LogError(ex, "فشل حذف المستخدم {UserId} بسبب تعارض في البيانات", profile.UserId);
                            return false;
                        }

                        if (!userDeleteResult.Succeeded)
                        {
                            //_logger.LogWarning("فشل حذف المستخدم المرتبط بالبروفايل {ProfileId}: {Errors}",
                            //    profileId,
                            //    string.Join(", ", userDeleteResult.Errors.Select(e => e.Description)));
                            return false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
