using System.Net.Http.Json;
using Infra;
using LinqToDB;

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
        var url =
            $"https://nagerholidays.com/api/v4/Holidays/DK/{year}";

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
}

public record NagerHolidayResponse(
    DateOnly Date,
    string Name,
    bool NationalHoliday,
    string[] HolidayTypes);