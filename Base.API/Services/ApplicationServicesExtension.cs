using Base.API.Filters;
using Base.DAL.Contexts;
using Base.DAL.Models;
using Base.Repo.Implementations;
using Base.Repo.Interfaces;
using Base.Services.Implementations;
using Base.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

namespace Base.API.Services
{
    public static class ApplicationservicesExtension
    {
        public static IServiceCollection AddApplicationservices(this IServiceCollection services, IConfiguration _configuration)
        {
            #region Configure services
            // 1️⃣ ربط DbContext من DAL مع SQL Server
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(_configuration.GetConnectionString("DefaultConnection")));

            // 2️⃣ إعداد Identity
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // ... إعدادات المصادقة الأخرى ...
                options.SignIn.RequireConfirmedEmail = true; // نوصي بتفعيل هذا الخيار

            })
                .AddEntityFrameworkStores<AppDbContext>() // DbContext موجود في DAL
                .AddDefaultTokenProviders();

            // 🟩 هنا ضيفي إعدادات الـ Identity Options
            services.Configure<IdentityOptions>(options =>
            {
                // المستخدم لازم يؤكد الإيميل قبل تسجيل الدخول
                options.SignIn.RequireConfirmedEmail = true;

                // إعدادات الباسورد (اختياري)
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredUniqueChars = 0;
                options.Password.RequireNonAlphanumeric = false;
            });

            // 🚨 الخطوة الصحيحة: استخدام Configure لضبط خيارات مزود التوكنات الافتراضي 🚨
            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                // هنا يتم تعيين فترة الصلاحية المطلوبة.
                // مثال: جعل صلاحية التوكن 3 ساعات (بدلاً من الافتراضي 24 ساعة).
                options.TokenLifespan = TimeSpan.FromHours(3);
            });

            // ------------------------------------------------------------------
            // 3. Dependency Injection لتسجيل الخدمات
            // ------------------------------------------------------------------

            // تسجيل خدمة الذاكرة المؤقتة (IMemoryCache) - ضروري لـ OtpService
            services.AddMemoryCache();

            // تسجيل خدمة OTP
            services.AddScoped<IOtpService, OtpService>();
            // ربط Repository بـ Dependency Injection
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddTransient<IEmailSender, EmailService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAuthService, AuthService>();

            // 💡 إضافة Caching للتحكم في استجابات المتصفحات (وقائي)
            services.AddResponseCaching();

            // 💡 إضافة Response Compression لتقليل حجم البيانات المنقولة (كفاءة)
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.MimeTypes = new[] { "application/json", "text/plain" };
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder =>
                    {
                        // 💡 قم بتغيير هذا لعنوان الـ Frontend الخاص بك في بيئة الإنتاج!
                        builder.WithOrigins("*")//("http://localhost:3000", "https://yourfrontenddomain.com")
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                        //.AllowCredentials(); // ضروري إذا كنت تستخدم الكوكيز/الـ OAuth
                    });
            });

            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme) // للـ OAuth
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = _configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = _configuration["Authentication:Google:ClientSecret"];
                googleOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddFacebook(facebookOptions =>
            {
                facebookOptions.AppId = _configuration["Authentication:Facebook:AppId"];
                facebookOptions.AppSecret = _configuration["Authentication:Facebook:AppSecret"];
                facebookOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

            services.AddEndpointsApiExplorer();
            //services.AddSwaggerGen();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "My API",
                    Version = "v1",
                    Description = "API documentation with unified response format"
                });

                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
                // ✅ نربط كل أكشن بالـ response الفعلي بتاعه
                options.OperationFilter<SwaggerResponseOperationFilter>();
            });

            #endregion
            return services;
        }
    }
}
