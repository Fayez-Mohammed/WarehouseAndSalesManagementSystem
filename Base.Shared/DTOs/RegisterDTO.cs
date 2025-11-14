using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Base.Shared.DTOs

{
    public class RegisterDTO
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [PasswordPropertyText]
        public required string Password { get; set; }

        [Required]
        public required string FullName { get; set; }

        //[Required]
        //public required string UserType { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

    }
}
