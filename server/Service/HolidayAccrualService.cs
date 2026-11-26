using System.ComponentModel.DataAnnotations;
using Infra;
using LinqToDB;
using Service.Security;

namespace Service;

public class HolidayAccrualService(
    MyDatabaseConnection db,
    WorkingScheduleService workingScheduleService,
    GlobalRuleSetService globalRuleSetService,
    RulePresetService rulePresetService,
    HolidayAccountService holidayAccountService)
{
    private decimal CalculateDailyAccrual(
        string userId,
        string accountType,
        DateOnly date)
    {
        if (accountType is not (
            HolidayAccount.Statutory or HolidayAccount.SixthWeek))
            throw new ValidationException("Invalid holiday account type");

        var rules = globalRuleSetService
            .GetApplicableRulesSet(date);

        if (accountType == HolidayAccount.SixthWeek)
        {
            var preset = rulePresetService.GetApplicablePreset(date);

            if (preset.RulePresetCode != RulePreset.KlSixthWeek)
                return 0m;
        }

        decimal weeklyHours = workingScheduleService.GetWeeklyHoursForDate(userId, date);
        
        int daysIsMonth = DateTime.DaysInMonth(date.Year, date.Month);

        if (accountType == HolidayAccount.Statutory)
        {
            decimal annualHours = rules.StatutoryAnnualDays / 5m * weeklyHours;
            
            return annualHours / 12 / daysIsMonth;
        }

        decimal sixthWeekAnnualHours =
            rules.SixthWeekAnnualHours =
                weeklyHours /
                rules.FullTimeWeeklyHours;
        
        return sixthWeekAnnualHours / 12m / daysIsMonth;


    }

    public decimal CalculateAccruedHours(
        string userId,
        string accountId,
        DateOnly throughDate)
    {
        var account = db.HolidayAccounts
            .FirstOrDefault(a=>
                a.HolidayAccountId == accountId &&
                a.UserId == userId);
        
        if (account is null)
            throw new ValidationException("Holiday account not found");

        var firstSchedule = db.WorkingSchedules
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.ValidFrom)
            .FirstOrDefault();
        
        if (firstSchedule is null)
            throw new ValidationException("Employee has no working schedule");

        var startDate = account.EarningPeriodStart;
        
        if (firstSchedule.ValidFrom > startDate)
            startDate = firstSchedule.ValidFrom;

        var endDate = throughDate;

        if (endDate > account.EarningPeriodEnd)
            endDate = account.EarningPeriodEnd;

        if (endDate < startDate)
            return 0m;

        decimal total = 0m;

        for (var date = startDate;
             date <= endDate;
             date = date.AddDays(1))
        {
            total += CalculateDailyAccrual(
                userId,
                account.AccountType,
                date);
        }

        return total;
    }

    public List<HolidayAccountEntryDto> PostCompletedMonth(string userId, DateOnly monthStart)
    {
        if (monthStart.Day != 1)
            throw new ValidationException("Month must begin on the first day");

        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        
        var timezone = TimeZoneInfo.FindSystemTimeZoneById(
            "Europe/Copenhagen");

        var danishTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow, timezone);

        var today = DateOnly.FromDateTime(danishTime);

        if (monthEnd >= today)
            throw new ValidationException("Only completed months can be posted");
        
        if (!db.Users.Any(u=>
                u.UserId == userId &&
                u.Role == UserRoles.Employee))
            throw new ValidationException("Employee not found");

        var firstSchedule = db.WorkingSchedules
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.ValidFrom)
            .FirstOrDefault();
        
        if (firstSchedule is null)
            throw new ValidationException("Employee has not working schedule");

        if (monthEnd < firstSchedule.ValidFrom)
            return [];

        var startDate = monthStart;

        if (startDate < firstSchedule.ValidFrom)
            startDate = firstSchedule.ValidFrom;

        var totals = new Dictionary<string,
            (decimal Hours, DateOnly LastDate)>();

        using var transaction = db.BeginTransaction();

        for (var date = startDate;
             date <= monthEnd;
             date = date.AddDays(1))
        {
            holidayAccountService.EnsureAccountsForDate(userId, date);

            foreach (var accountType in new[]
                     {
                         HolidayAccount.Statutory,
                         HolidayAccount.SixthWeek
                     })
            {
                decimal hours = CalculateDailyAccrual(
                    userId, accountType, date);
                
                if (hours == 0m)
                    continue;
                
                var account = db.HolidayAccounts
                    .FirstOrDefault(a=>
                        a.UserId == userId &&
                        a.AccountType == accountType &&
                        a.EarningPeriodStart <= date &&
                        a.EarningPeriodEnd >= date);
                
                if (account is null)
                    throw new ValidationException("Required holiday account not found");

                var previous = totals.GetValueOrDefault(
                    account.HolidayAccountId);

                totals[account.HolidayAccountId] = (
                    previous.Hours + hours,
                    date);
            }
        }

        var createdEntries = new List<HolidayAccountEntryDto>();

        string reference = $"ACCRUAL: {monthStart:yyyy-MM}";

        foreach (var (accountId, value) in totals)
        {
            bool alreadyPosted = db.HolidayAccountEntries.Any(e =>
                e.HolidayAccountId == accountId &&
                e.EntryType == "ACCRUAL" &&
                e.ReferenceId == reference);
            
            if (alreadyPosted)
                continue;

            var entry = new HolidayAccountEntry
            {
                HolidayAccountEntryId = Guid.NewGuid().ToString(),
                HolidayAccountId = accountId,
                EffectiveDate = value.LastDate,
                EntryType = "ACCRUAL",
                Hours = value.Hours,
                ReferenceId = reference,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Insert(entry);
            createdEntries.Add(new HolidayAccountEntryDto(entry));
        }
        transaction.Commit();
        return createdEntries;
        
    }
    
}