using System.ComponentModel.DataAnnotations;

namespace Base.Shared.DTOs
{
    public class ChangePasswordDTO
    {
        [Required]
        public required string CurrentPassword { get; set; }
        [Required]
        public required string NewPassword { get; set; }
    }
}
