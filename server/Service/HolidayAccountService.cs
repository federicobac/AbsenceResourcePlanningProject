using System.ComponentModel.DataAnnotations;
using Infra;
using LinqToDB;
using Service.Security;

namespace Service;

public class HolidayAccountService(
    MyDatabaseConnection db,
    GlobalRuleSetService globalRuleSetService,
    RulePresetService rulePresetService)
{
    private static DateOnly GetPeriodStart(DateOnly date, int startMonth, int startDay)
    {
        var start = new DateOnly(date.Year, startMonth, startDay);

        if (start > date)
            start = start.AddYears(-1);
        
        return start;
    }

    public static DateOnly GetNextDate(DateOnly date, int month, int day)
    {
        var result = new DateOnly(date.Year, month, day);

        if (result < date)
            result = result.AddYears(1);

        return result;
    }

    private void EnsureAccount(
        string userId,
        string accountType,
        DateOnly earningStart,
        DateOnly earningEnd,
        DateOnly usageStart,
        DateOnly usageEnd)
    {
        bool exists = db.HolidayAccounts.Any(a =>
            a.UserId == userId &&
            a.AccountType == accountType &&
            a.EarningPeriodStart == earningStart);

        if (exists)
            return;

        db.Insert(new HolidayAccount
        {
            HolidayAccountId = Guid.NewGuid().ToString(),

            UserId = userId,
            AccountType = accountType,

            EarningPeriodStart = earningStart,
            EarningPeriodEnd = earningEnd,

            UsagePeriodStart = usageStart,
            UsagePeriodEnd = usageEnd
        });
    }

    public void EnsureAccountsForDate(string userId, DateOnly date)
    {
        var user = db.Users
            .FirstOrDefault(u => u.UserId == userId);

        if (user is null)
            throw new ValidationException("User not found");

        if (user.Role != UserRoles.Employee)
            throw new ValidationException("Holiday accounts can be created for employees");

        var rules = globalRuleSetService
            .GetApplicableRulesSet(date);

        var preset = rulePresetService
            .GetApplicablePreset(date);

        var statutoryStart = GetPeriodStart(
            date,
            rules.StatutoryEarningStartMonth,
            rules.StatutoryEarningStartDay);

        var statutoryEnd = statutoryStart
            .AddYears(1)
            .AddDays(-1);

        var statutoryUsageEnd = GetNextDate(
            statutoryEnd,
            rules.StatutoryUsageEndMonth,
            rules.StatutoryUsageEndDay);

        EnsureAccount(
            userId,
            HolidayAccount.Statutory,
            statutoryStart,
            statutoryEnd,
            statutoryStart,
            statutoryUsageEnd);

        if (preset.RulePresetCode == RulePreset.KlSixthWeek)
        {
            var sixthWeekStart = GetPeriodStart(
                date,
                rules.SixthWeekEarningStartMonth,
                rules.SixthWeekEarningStartDay);

            var sixthWeekEnd = sixthWeekStart
                .AddYears(1)
                .AddDays(-1);

            var sixthWeekUsageStart = GetNextDate(
                sixthWeekEnd.AddDays(1),
                rules.SixthWeekUsageStartMonth,
                rules.SixthWeekUsageStartDay);

            var sixthWeekUsageEnd = GetNextDate(
                sixthWeekUsageStart,
                rules.SixthWeekUsageEndMonth,
                rules.SixthWeekUsageEndDay);

            EnsureAccount(
                userId,
                HolidayAccount.SixthWeek,
                sixthWeekStart,
                sixthWeekEnd,
                sixthWeekUsageStart,
                sixthWeekUsageEnd);
        }
    }

    public List<HolidayAccountDto> GetAccounts(string userId)
    {
        if (!db.Users.Any(u => u.UserId == userId))
            throw new ValidationException("User not found");
        
        return db.HolidayAccounts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.EarningPeriodStart)
            .ToList()
            .Select(a => new HolidayAccountDto(a))
            .ToList();
    }

    public decimal GetBalance(string accountId)
    {
        if (!db.HolidayAccounts.Any(a => a.HolidayAccountId == accountId))
        { throw new ValidationException("Holiday account not found"); }

        return db.HolidayAccountEntries
            .Where(e => e.HolidayAccountId == accountId)
            .Select(e => e.Hours)
            .ToList()
            .Sum();
    }

    public decimal GetRecordedBalanceOnDate(
        string userId,
        string accountType,
        DateOnly date)
    {
        if (!db.Users.Any(u => u.UserId == userId && u.Role == UserRoles.Employee))
        { throw new ValidationException("Employee not found"); }

        if (accountType is not (HolidayAccount.Statutory or HolidayAccount.SixthWeek))
        { throw new ValidationException("Invalid holiday account type"); }

        var accountIds = db.HolidayAccounts
            .Where(a =>
                a.UserId == userId &&
                a.AccountType == accountType &&
                a.UsagePeriodStart <= date &&
                a.UsagePeriodEnd >= date)
            .Select(a => a.HolidayAccountId)
            .ToList();

        if (accountIds.Count == 0)
            return 0m;

        return db.HolidayAccountEntries
            .Where(e =>
                accountIds.Contains(e.HolidayAccountId) &&
                e.EffectiveDate <= date)
            .Select(e => e.Hours)
            .ToList()
            .Sum();
    }

    public List<HolidayAccount> GetProjectionAccounts(string userId, DateOnly targetDate)
    {
        if (!db.Users.Any(u =>
                u.UserId == userId &&
                u.Role == UserRoles.Employee))
            throw new ValidationException("Employee not found");

        var accounts = db.HolidayAccounts
            .Where(a => a.UserId == userId)
            .ToList();
        
        // Earliest date for which both configuration systems exist
        var firstRules = db.GlobalRuleSets
            .OrderBy(r => r.ValidFrom)
            .FirstOrDefault();

        var firstPreset = db.RulePresetPeriods
            .OrderBy(p => p.ValidFrom)
            .FirstOrDefault();

        if (firstRules is null || firstPreset is null)
            throw new ValidationException("Holiday configuration is missing");

        var earliestDate =
            firstRules.ValidFrom > firstPreset.ValidFrom
                ? firstRules.ValidFrom
                : firstPreset.ValidFrom;

        if (targetDate < earliestDate)
            return [];
        
        // Sample relevant earning years and configuration transitions
        var dates = new HashSet<DateOnly>
        {
            targetDate,
            earliestDate
        };
        
        for (int i = 1; i <= 3 && targetDate.Year > i; i++) 
            dates.Add(targetDate.AddYears(-i));
        
        foreach (var rules in db.GlobalRuleSets
                     .Where(r => r.ValidFrom <= targetDate)
                     .ToList())
        { 
            dates.Add(rules.ValidFrom);
        }
        
        foreach (var period in db.RulePresetPeriods
                     .Where(p => p.ValidFrom <= targetDate)
                     .ToList())
        { 
            dates.Add(period.ValidFrom);
        }

        foreach (var date in dates
                     .Where(d => d >= earliestDate && d <= targetDate)
                     .OrderBy(d => d))
        {
            var rules = globalRuleSetService
                .GetApplicableRulesSet(date);

            var preset = rulePresetService
                .GetApplicablePreset(date);
            
            // Statutory
            var statutoryStart = GetPeriodStart(
                date,
                rules.StatutoryEarningStartMonth,
                rules.StatutoryEarningStartDay);

            var statutoryEnd = statutoryStart
                .AddYears(1)
                .AddDays(-1);

            var statutoryUsageEnd = GetNextDate(
                statutoryEnd,
                rules.StatutoryUsageEndMonth,
                rules.StatutoryUsageEndDay);

            AddCandidate(
                HolidayAccount.Statutory,
                statutoryStart,
                statutoryEnd,
                statutoryStart,
                statutoryUsageEnd);
            
            // Sixth week
            if (preset.RulePresetCode == RulePreset.KlSixthWeek)
            {
                var sixthStart = GetPeriodStart(
                    date,
                    rules.SixthWeekEarningStartMonth,
                    rules.SixthWeekEarningStartDay);

                var sixthEnd = sixthStart
                    .AddYears(1)
                    .AddDays(-1);

                var usageStart = GetNextDate(
                    sixthEnd.AddDays(1),
                    rules.SixthWeekUsageStartMonth,
                    rules.SixthWeekUsageStartDay);

                var usageEnd = GetNextDate(
                    usageStart,
                    rules.SixthWeekUsageEndMonth,
                    rules.SixthWeekUsageEndDay);
                
                AddCandidate(
                    HolidayAccount.SixthWeek,
                    sixthStart,
                    sixthEnd,
                    usageStart,
                    usageEnd);
            }
        }

        return accounts
            .Where(a =>
                a.UsagePeriodStart <= targetDate &&
                a.UsagePeriodEnd >= targetDate)
            .ToList();
        
        // Add an in-memory account only if the earning period is not already represented
        void AddCandidate(
            string accountType,
            DateOnly earningStart,
            DateOnly earningEnd,
            DateOnly usageStart,
            DateOnly usageEnd)
        {
            if (accounts.Any(a =>
                    a.AccountType == accountType &&
                    a.EarningPeriodStart == earningStart))
                return;
            
            accounts.Add(new HolidayAccount
            {
                HolidayAccountId = string.Empty,
                UserId = userId,
                AccountType = accountType,
                
                EarningPeriodStart = earningStart,
                EarningPeriodEnd = earningEnd,
                
                UsagePeriodStart = usageStart,
                UsagePeriodEnd = usageEnd
            });
        }
    }
}