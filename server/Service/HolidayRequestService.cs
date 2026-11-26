using System.ComponentModel.DataAnnotations;
using Infra;

namespace Service;

public class HolidayRequestService(MyDatabaseConnection db)
{
    private static void ValidateAccountType(string accountType)
    {
        if (accountType is not (
            HolidayAccount.Statutory or HolidayAccount.SixthWeek))
        {
            throw new ValidationException("Invalid holiday account type");
        }
    }

    private static DateOnly Today()
    {
        var timezone = TimeZoneInfo.FindSystemTimeZoneById(
            "Europe/Copenhagen");

        var danishTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow, timezone);
        
        return DateOnly.FromDateTime(danishTime);
    }

    private static void ValidateDates(DateOnly startDate, DateOnly endDate)
    {
        if (startDate == default || endDate == default)
            throw new ValidationException("Start and end dates are required");
        
        if (endDate < startDate)
            throw new ValidationException("End date cannot be before start date");

        if (startDate < Today())
            throw new ValidationException("Holiday requests cannot start in the past");
    }

    private void ValidateNoOverlap(
        string userId,
        DateOnly startDate,
        DateOnly endDate)
    {
        bool overlaps = db.HolidayRequests.Any(r =>
            r.UserId == userId &&

            (r.Status == HolidayRequest.Pending ||
             r.Status == HolidayRequest.Approved) &&

            r.StartDate <= endDate &&
            r.EndDate >= startDate);

        if (overlaps)
            throw new ValidationException(
                "An existing holiday request overlaps these dates");
    }
    
    
    
}