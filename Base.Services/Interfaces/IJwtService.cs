using Base.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Interfaces
{
    public interface IJwtService
    {
        Task<string> GenerateJwtTokenAsync(ApplicationUser user); // user from identity/your domain
    }
}
