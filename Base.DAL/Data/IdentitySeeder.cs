using Base.DAL.Contexts;
using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels;
using Base.Shared.DTOs;
using Base.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Base.DAL.Seeding
{
    public static class IdentitySeeder
    {
        public static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            var roleNames = Enum.GetNames<UserTypes>();
            // ✅ تأكد من وجود كل Role
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // 🧑‍💼 بيانات الأدمن الافتراضي
            string adminEmail = "islam7lmy@gmail.com";
            string adminPassword = "Admin@123";

            // ✅ تحقق لو الأدمن مش موجود
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    FullName = "Islam helmy",
                    Type = UserTypes.SystemAdmin,
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    // 🟣 أضف الأدمن إلى دور "Admin"
                    await userManager.AddToRoleAsync(adminUser, UserTypes.SystemAdmin.ToString());
                }
            }
        }
    }
}