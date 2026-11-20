using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Infra;
using LinqToDB;
using Service.RequestDtos;

namespace Service;

public class HolidayDateService(
    MyDatabaseConnection db,
    HttpClient httpClient)
{
    public List<HolidayDateDto> GetHolidays(int year)
    {
        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);

        return db.HolidayDates
            .Where(h => h.Date >= start && h.Date <= end)
            .OrderBy(h => h.Date)
            .ToList()
            .Select(h => new HolidayDateDto(h))
            .ToList();
    }

    public async Task<int> ImportPublicHolidays(int year)
    {
        var url = $"https://nagerholidays.com/api/v4/Holidays/DK/{year}";

        var holidays = await httpClient
            .GetFromJsonAsync<List<NagerHolidayResponse>>(url);

        if (holidays is null)
            throw new Exception("Could not retrieve Danish holidays");

        int imported = 0;

        foreach (var holiday in holidays)
        {
            if (!holiday.NationalHoliday ||
                !holiday.HolidayTypes.Contains("Public"))
                continue;

            bool exists = db.HolidayDates.Any(h =>
                h.Date == holiday.Date &&
                h.Type == "Public");

            if (exists)
                continue;

            db.Insert(new HolidayDate
            {
                HolidayDateId = Guid.NewGuid().ToString(),
                Date = holiday.Date,
                Name = holiday.Name,
                Type = "Public"
            });

            imported++;
        }

        return imported;
    }

    public HolidayDateDto CreateCompanyHoliday(
        CreateCompanyHolidayRequestDto dto)
    { 
        if (dto.Date == default)
            throw new ValidationException("Date is required");
                
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ValidationException("Name is required");

        if (db.HolidayDates.Any(h => h.Date == dto.Date && h.Type == "company"))
            throw new ValidationException("Company holiday already exists on this date");

        var holiday = new HolidayDate
        {
            HolidayDateId = Guid.NewGuid().ToString(),
            Date = dto.Date,
            Name = dto.Name.Trim(),
            Type = "Company"
        };
        db.Insert(holiday);
        
        return new HolidayDateDto(holiday);

    }

    public HolidayDateDto UpdateCompanyHoliday(string id, UpdateCompanyHolidayRequestDto dto)
    {
        var holiday = db.HolidayDates
            .FirstOrDefault(h => h.HolidayDateId == id);
        
        if (holiday is null || holiday.Type != "Company" )
            throw new ValidationException("Company holiday not found");
        
        if (dto.Date == default)
            throw new ValidationException("Date is required");
        
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ValidationException("Name is required");
        
        if (db.HolidayDates.Any(h =>
                h.Date == dto.Date &&
                h.Type == "company" &&
                h.HolidayDateId != id))
            throw new ValidationException("Company holiday already exists on this date");
        
        holiday.Date = dto.Date;
        holiday.Name = dto.Name.Trim();
        
        db.Update(holiday);
        
        return new HolidayDateDto(holiday);
    }

    public void DeleteCompanyHoliday(string id)
    {
        var holiday = db.HolidayDates
            .FirstOrDefault(h => h.HolidayDateId == id);
        
        if (holiday is null|| holiday.Type != "Company")
            throw new ValidationException("Company holiday not found");
        
        db.Delete(holiday);
    }

    public bool IsNonWorkingDay(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return true;

        return db.HolidayDates.Any(h => h.Date == date);
    }
}

public record NagerHolidayResponse(
    DateOnly Date, 
    string Name, 
    bool NationalHoliday, 
    string[] HolidayTypes);
