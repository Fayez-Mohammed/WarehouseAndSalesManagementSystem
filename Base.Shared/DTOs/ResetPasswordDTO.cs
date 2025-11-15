using System.ComponentModel.DataAnnotations;

namespace Base.Shared.DTOs
{
    public class ResetPasswordDTO
    {
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string Token { get; set; }
        [Required]
        public required string NewPassword { get; set; }
    }

    public class VerifyForgetPasswordDTO
    {
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string Otp { get; set; }
        //[Required]
        //public required string NewPassword { get; set; }
    }
}
