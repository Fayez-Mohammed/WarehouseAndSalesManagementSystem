using System.ComponentModel.DataAnnotations;

namespace Base.Shared.DTOs
{
    public class VerifyOtpDTO
    {
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string Otp { get; set; }
    }
}
