using Base.DAL.Models;
using Base.Repo.Interfaces;
using RepositoryProject.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Helpers
{
    public class AppointmentSlotGeneratorJob
    {
        private readonly IUnitOfWork _unitOfWork;

        public AppointmentSlotGeneratorJob(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task GenerateMonthlySlotsAsync()
        {
            var scheduleRepo = _unitOfWork.Repository<ClinicSchedule>();
            var slotRepo = _unitOfWork.Repository<AppointmentSlot>();

            var allSchedules = await scheduleRepo.ListAllAsync();

            var now = DateTime.UtcNow; // وقت النظام

            var startDate = now.Date;
            var endDate = startDate.AddMonths(1);

            foreach (var schedule in allSchedules)
            {
                // لكل يوم خلال الشهر
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != schedule.Day)
                        continue;

                    var dayStart = date.Add(schedule.StartTime);
                    var dayEnd = date.Add(schedule.EndTime);

                    for (var slotStart = dayStart;
                         slotStart < dayEnd;
                         slotStart = slotStart.AddMinutes(schedule.SlotDurationMinutes))
                    {
                        var slotEnd = slotStart.AddMinutes(schedule.SlotDurationMinutes);

                        // تحقق إن الميعاد مش موجود مسبقًا
                        var spec = new BaseSpecification<AppointmentSlot>(s =>
                            s.ClinicScheduleId == schedule.Id &&
                            s.StartTime == TimeSpan.Parse(slotStart.ToString()));
                        var exists = await slotRepo.CountAsync(spec);
                        if (exists < 1)
                        {
                            var newSlot = new AppointmentSlot
                            {
                                Id = Guid.NewGuid().ToString(),
                                ClinicScheduleId = schedule.Id,
                                StartTime = TimeSpan.Parse(slotStart.ToString()),
                                EndTime = TimeSpan.Parse(slotEnd.ToString()),
                                IsBooked = false
                            };
                            await slotRepo.AddAsync(newSlot);
                        }
                    }
                }
            }

            await _unitOfWork.CompleteAsync();
        }
    }

}
