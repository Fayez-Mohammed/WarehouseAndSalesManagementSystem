using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Models
{
    public class SystemAdminProfile : BaseEntity
    {
        //public int Id { get; set; }
        public string? UserId { get; set; }

        // Navigation property
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }
    }

    public class ClincAdminProfile : BaseEntity
    {
        public string? UserId { get; set; }
        public string? ClincId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey(nameof(ClincId))]
        public virtual Clinic? Clinc { get; set; }
    }

    public class ClincDoctorProfile : BaseEntity
    {
        public string? UserId { get; set; }
        public string? ClincId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey(nameof(ClincId))]
        public virtual Clinic? Clinc { get; set; }

        public virtual ICollection<ClinicSchedule> Schedules { get; set; }

    }

    public class ClincReceptionistProfile : BaseEntity
    {
        public string? UserId { get; set; }
        public string? ClincId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey(nameof(ClincId))]
        public virtual Clinic? Clinc { get; set; }
    }

    public class Clinic : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AddressCountry { get; set; }
        public string? AddressGovernRate { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressLocation { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; }
        public double Price { get; set; }
        public string? LogoPath { get; set; }

        //public ICollection<ClincProfile> ClincAdmin { get; set; } = new HashSet<ClincProfile>();

        public virtual ICollection<ClincAdminProfile> ClincAdmin { get; set; } = new HashSet<ClincAdminProfile>();
        public virtual ICollection<ClincDoctorProfile> ClincDoctor { get; set; } = new HashSet<ClincDoctorProfile>();
        public virtual ICollection<ClincReceptionistProfile> ClincReceptionis { get; set; } = new HashSet<ClincReceptionistProfile>();
        public string? MedicalSpecialtyId { get; set; }
        public virtual MedicalSpecialty? MedicalSpecialty { get; set; }

        public virtual ICollection<ClinicSchedule> ClinicSchedules { get; set; } = new List<ClinicSchedule>();

    }

    public class MedicalSpecialty : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UserType : BaseEntity
    {
        public string Name { get; set; }
    }

    public class ClinicSchedule : BaseEntity
    {
        public string ClinicId { get; set; }
        public virtual Clinic Clinic { get; set; }

        public string DoctorId { get; set; }
        public virtual ClincDoctorProfile Doctor { get; set; }

        public DayOfWeek Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public int SlotDurationMinutes { get; set; }

        public virtual ICollection<AppointmentSlot> AppointmentSlots { get; set; } = new List<AppointmentSlot>();
    }

    public class AppointmentSlot : BaseEntity
    {
        public string ClinicScheduleId { get; set; }
        public virtual ClinicSchedule DoctorSchedule { get; set; }

        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBooked { get; set; }

        public virtual Appointment? Appointment { get; set; }
    }

    public class Appointment : BaseEntity
    {
        public string SlotId { get; set; }
        public virtual AppointmentSlot Slot { get; set; }
        public string PatientId { get; set; }
        public virtual UserProfile Patient { get; set; }
        public string Reason { get; set; }
    }


    /*public class ClincProfile : BaseEntity
    {
        public string? UserId { get; set; }
        public string? ClincId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [ForeignKey(nameof(ClincId))]
        public Clinic? Clinc { get; set; }
    }*/
}
