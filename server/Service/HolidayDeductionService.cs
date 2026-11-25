using System.ComponentModel.DataAnnotations;
using Infra;
using Service.Security;

namespace Service;

public class HolidayDeductionService(
    MyDatabaseConnection db,
    WorkingScheduleService workingScheduleService,
    HolidayDateService holidayDateService)
{
    public HolidayDeductionPreviewDto PreviewDeduction(
        string userId,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (startDate == default || endDate == default)
            throw new ValidationException("Start and end dates are required");
        
        if (endDate < startDate )
            throw new ValidationException("End date cannot be before start date");

        if (!db.Users.Any(u =>
                u.UserId == userId &&
                u.Role == UserRoles.Employee))
        {
            throw new ValidationException("Employee not found");
        }

        var days = new List<HolidayDeductionDayDto>();

        for (var date = startDate;; date = date.AddDays(1))
        {
            if (!holidayDateService.IsNonWorkingDay(date))
            {
                decimal hours =
                    workingScheduleService.GetScheduledHoursForDate(
                        userId,
                        date);

                if (hours > 0)
                {
                    days.Add(new HolidayDeductionDayDto(date, hours));
                }

            }
            if (date == endDate) break;
        }
        if (days.Count == 0)
            throw new ValidationException("The selected period contains no working days");
        
        return new HolidayDeductionPreviewDto(
            userId,
            startDate,
            endDate,
            days.Count,
            days.Sum(d=>d.Hours),
            days);
            
    }
}