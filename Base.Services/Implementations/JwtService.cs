using Base.DAL.Models.BaseModels;
using Base.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Implementations
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly IUserService _userService;

        public JwtService(IConfiguration config, IUserService userService)
        {
            _config = config;
            _userService = userService;
        }

        public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            // استخدم asymmetric (RS256) إن أمكن. هنا مثال مختصر بسymmetric:
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Auth:Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("UserType", user.UserType ?? string.Empty)
            // ... إضافات أخرى ضرورية مثل roles
        };
            // Get and add Roles
            //var roles = await _userManager.GetRolesAsync(user);
            var roles = await _userService.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            if (!int.TryParse(_config["Auth:Jwt:Minutes"], out int minutes))
            {
                minutes = 60; // قيمة افتراضية إذا كانت الإعدادات خاطئة
            }
            var token = new JwtSecurityToken(
                issuer: _config["Auth:Jwt:Issuer"],
                audience: _config["Auth:Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: creds
            );

            return await Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
        }


        public async Task<ClaimsPrincipal> ValidateTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Auth:Jwt:Key"]);
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _config["Auth:Jwt:Issuer"],
                ValidAudience = _config["Auth:Jwt:Audience"],
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }

        public async Task<string> GetUserIdFromTokenAsync(string token)
        {
            var principal = await ValidateTokenAsync(token);
            return principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
    }
}
