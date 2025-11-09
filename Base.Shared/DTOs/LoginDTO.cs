using System.ComponentModel.DataAnnotations;

namespace Base.Shared.DTOs
{
    public class LoginDTO
    {
        [Required]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public bool RequiresOtpVerification { get; set; } = false;
        public bool EmailConfirmed { get; set; } = false;
        public string Message { get; set; }

        public LoginResponse? Data { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public object user { get; set; }
    }
}
