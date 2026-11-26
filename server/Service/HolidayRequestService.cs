using System.ComponentModel.DataAnnotations;
using Infra;
using Service.RequestDtos;
using Service.Security;

namespace Service;

public class HolidayRequestService(
    MyDatabaseConnection db,
    HolidayDeductionService deductionService)
{ 
    private static DateOnly Today()
    {
        var timezone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

        var danishTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);

        return DateOnly.FromDateTime(danishTime);
    }
    
    private static void ValidateAccountType(string accountType)
    {
        if (accountType is not (HolidayAccount.Statutory or HolidayAccount.SixthWeek))
        {
            throw new ValidationException("Invalid holiday account type");
        }
    }

    public static void ValidateDates(DateOnly startDate, DateOnly endDate)
    {
        if (startDate == default || endDate == default) 
            throw new ValidationException("Start and end dates are required");
        
        if (endDate < startDate)
            throw new ValidationException("End date cannot be before start date");

        if (startDate < Today())
            throw new ValidationException("Holiday requests cannot start in the past");
    }

    private void ValidateNoOverlap(string userId, DateOnly startDate, DateOnly endDate)
    {
        bool overlaps = db.HolidayRequests.Any(r => 
            r.UserId == userId && 
            (r.Status == HolidayRequest.Pending || r.Status == HolidayRequest.Approved) &&
            r.StartDate <= endDate && r.EndDate >= startDate);

        if (overlaps)
            throw new ValidationException("An existing holiday request overlaps these dates");
    }

    public HolidayDeductionPreviewDto PreviewRequest(
        string userId,
        CreateHolidayRequestRequestDto dto)
    {
        if (!db.Users.Any(u => u.UserId == userId && u.Role == UserRoles.Employee))
        { throw new ValidationException("Employee not found"); }
        
        //Validate request
        ValidateAccountType(dto.AccountType);
        ValidateDates(dto.StartDate, dto.EndDate);
        ValidateNoOverlap(userId, dto.StartDate, dto.EndDate);
        
        // Calculate requested hours
        return deductionService.PreviewDeduction(userId, dto.StartDate, dto.EndDate);
    }
}