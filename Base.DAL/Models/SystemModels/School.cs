using Base.DAL.Models.BaseModels;

namespace Base.DAL.Models.SystemModels
{
    public class School : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AddressCountry { get; set; }
        public string? AddressGovernRate { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressLocation { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; }
        public string? LogoPath { get; set; }
        public virtual ICollection<SchoolAdminProfile> SchoolAdmin { get; set; } = new HashSet<SchoolAdminProfile>();
        
    }
}
