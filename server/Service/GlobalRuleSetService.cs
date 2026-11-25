using System.ComponentModel.DataAnnotations;
using Infra;
using LinqToDB;
using Service.RequestDtos;

namespace Service;

public class GlobalRuleSetService(MyDatabaseConnection db)
{
    // Returns the current calendar date (DateOnly) in the Danish time zone
    private static DateOnly Today()
    {
        var timezone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

        var danishTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        
        return DateOnly.FromDateTime(danishTime);
    }

    // Validate whether a rule set can be modified
    private static void EnsureScheduled(GlobalRuleSet ruleSet)
    {
        if (ruleSet.ValidFrom <= Today())
            throw new ValidationException(
                "This rule set is already active or historical " +
                "and cannot be modified or deleted");
    }

    // Validate configuration values set by Admin (f.e. avoid 31st September)
    private static void ValidateMonthDay(
        int month,
        int day,
        string parameter)
    {
        if (month < 1 || month > 12 || 
            day < 1 || day > DateTime.DaysInMonth(2025, month))
        {
            throw new ValidationException($"Invalid date for {parameter}");
        }
    }
    
    // Validate the GlobalRuleSet
    private static void ValidateRuleSet(GlobalRuleSet ruleSet)
    {
        if (string.IsNullOrWhiteSpace(ruleSet.Name))
            throw new ValidationException("Name is required");
        
        if (ruleSet.FullTimeWeeklyHours <= 0)
            throw new ValidationException("Full time weekly hours must be greater than zero");
        
        if (ruleSet.StatutoryAnnualDays <= 0)
            throw new ValidationException("Statutory annual days must be greater than zero");
        
        if (ruleSet.MainHolidayDays <= 0 ||
            ruleSet.MainHolidayDays > ruleSet.StatutoryAnnualDays) 
            throw new ValidationException("Invalid number of main holiday days");
        
        if (ruleSet.SixthWeekAnnualHours < 0)
            throw new ValidationException("Sixth-week annal hours must be greater than zero");
        
        if (ruleSet.MaxStatutoryDebtDays < 0)
            throw new ValidationException("Maximum statutory debt cannot be negative");
        
        if (ruleSet.MainHolidayNoticeMonths < 0 || ruleSet.OtherHolidayNoticeMonths < 0)
            throw new ValidationException("Notice periods cannot be negative");
        
        ValidateMonthDay(
            ruleSet.StatutoryEarningStartMonth,
            ruleSet.StatutoryEarningStartDay,
            "Statutory earning start");
        
        ValidateMonthDay(
            ruleSet.StatutoryUsageEndMonth,
            ruleSet.StatutoryUsageEndDay,
            "Statutory usage end");
        
        ValidateMonthDay(
            ruleSet.MainHolidayStartMonth,
            ruleSet.MainHolidayStartDay,
            "Main holiday start");
        
        ValidateMonthDay(
            ruleSet.MainHolidayEndMonth,
            ruleSet.MainHolidayEndDay,
            "Main holiday end");
        
        ValidateMonthDay(
            ruleSet.SixthWeekEarningStartMonth,
            ruleSet.SixthWeekEarningStartDay,
            "Sixth-week earning start");
        
        ValidateMonthDay(
            ruleSet.SixthWeekUsageStartMonth,
            ruleSet.SixthWeekUsageStartDay,
            "Sixth-week usage start");
        
        ValidateMonthDay(
            ruleSet.SixthWeekUsageEndMonth,
            ruleSet.SixthWeekUsageEndDay,
            "Sixth-week usage end");
        
        ValidateMonthDay(
            ruleSet.FifthWeekTransferDeadlineMonth,
            ruleSet.FifthWeekTransferDeadlineDay,
            "Fifth-week transfer deadline");
        
        ValidateMonthDay(
            ruleSet.SixthWeekTransferDeadlineMonth,
            ruleSet.SixthWeekTransferDeadlineDay,
            "Sixth-week transfer deadline");
        
        var mainHolidayStart = new DateOnly(
            2025, 
            ruleSet.MainHolidayStartMonth, 
            ruleSet.MainHolidayStartDay);
        
        var mainHolidayEnd = new DateOnly(
            2025,
            ruleSet.MainHolidayEndMonth,
            ruleSet.MainHolidayEndDay);
        
        if (mainHolidayEnd < mainHolidayStart)
            throw new ValidationException(
                "Main holiday end cannot be before its start");
    }

    // Retrieves configuration history
    public List<GlobalRuleSetDto> GetRuleSets()
    {
        return db.GlobalRuleSets
            .OrderBy(r => r.ValidFrom)
            .ToList()
            .Select(r => new GlobalRuleSetDto(r))
            .ToList();
    }

    // Retrieve the applicable rule set for calculating holidays
    public GlobalRuleSetDto GetApplicableRulesSet(DateOnly date)
    {
        var ruleSet = db.GlobalRuleSets
            .Where(r =>
                r.ValidFrom <= date &&
                (!r.ValidUntil.HasValue || r.ValidUntil.Value >= date))
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefault();

        if (ruleSet is null)
            throw new ValidationException("No global rule set for the specified date");
        
        return new GlobalRuleSetDto(ruleSet);
    }
    
    private static GlobalRuleSet CreateEntity(CreateGlobalRuleSetRequestDto dto)
    {
        return new GlobalRuleSet
        {
            GlobalRuleSetId = Guid.NewGuid().ToString(),

            Name = dto.Name.Trim(),

            ValidFrom = dto.ValidFrom,
            ValidUntil = null,

            FullTimeWeeklyHours = dto.FullTimeWeeklyHours,

            StatutoryAnnualDays = dto.StatutoryAnnualDays,
            StatutoryEarningStartMonth = dto.StatutoryEarningStartMonth,
            StatutoryEarningStartDay = dto.StatutoryEarningStartDay,
            StatutoryUsageEndMonth = dto.StatutoryUsageEndMonth,
            StatutoryUsageEndDay = dto.StatutoryUsageEndDay,

            MainHolidayDays = dto.MainHolidayDays,
            MainHolidayStartMonth = dto.MainHolidayStartMonth,
            MainHolidayStartDay = dto.MainHolidayStartDay,
            MainHolidayEndMonth = dto.MainHolidayEndMonth,
            MainHolidayEndDay = dto.MainHolidayEndDay,

            MainHolidayNoticeMonths = dto.MainHolidayNoticeMonths,
            OtherHolidayNoticeMonths = dto.OtherHolidayNoticeMonths,

            SixthWeekAnnualHours = dto.SixthWeekAnnualHours,

            SixthWeekEarningStartMonth = dto.SixthWeekEarningStartMonth,
            SixthWeekEarningStartDay = dto.SixthWeekEarningStartDay,

            SixthWeekUsageStartMonth = dto.SixthWeekUsageStartMonth,
            SixthWeekUsageStartDay = dto.SixthWeekUsageStartDay,

            SixthWeekUsageEndMonth = dto.SixthWeekUsageEndMonth,
            SixthWeekUsageEndDay = dto.SixthWeekUsageEndDay,

            MaxStatutoryDebtDays = dto.MaxStatutoryDebtDays,

            FifthWeekTransferDeadlineMonth = dto.FifthWeekTransferDeadlineMonth,
            FifthWeekTransferDeadlineDay = dto.FifthWeekTransferDeadlineDay,

            SixthWeekTransferDeadlineMonth = dto.SixthWeekTransferDeadlineMonth,
            SixthWeekTransferDeadlineDay = dto.SixthWeekTransferDeadlineDay
        };
    }

    public GlobalRuleSetDto CreateRuleSet(CreateGlobalRuleSetRequestDto dto)
    {
        if (dto.ValidFrom < Today())
            throw new ValidationException("A new rule set cannot start in the past");

        var ruleSets = db.GlobalRuleSets
            .OrderBy(r => r.ValidFrom)
            .ToList();

        // Only one scheduled configuration is allowed.
        if (ruleSets.Any(r => r.ValidFrom > Today()))
            throw new ValidationException(
                "A future rule set is already scheduled. " + 
                "Update or delete it before creating another.");

        if (ruleSets.Any(r => r.ValidFrom == dto.ValidFrom))
            throw new ValidationException("A rule set already starts on this date");

        var previous = ruleSets.LastOrDefault();

        if (previous != null && dto.ValidFrom <= previous.ValidFrom)
            throw new ValidationException("New rule set must start after the previous rule set");

        var ruleSet = CreateEntity(dto);

        ValidateRuleSet(ruleSet);

        using var transaction = db.BeginTransaction();

        if (previous != null)
        {
            previous.ValidUntil = dto.ValidFrom.AddDays(-1);
            db.Update(previous);
        }

        db.Insert(ruleSet);

        transaction.Commit();

        return new GlobalRuleSetDto(ruleSet);
    }
    
    private static void ApplyUpdates(
        GlobalRuleSet ruleSet,
        UpdateGlobalRuleSetRequestDto dto)
    {
        ruleSet.Name = dto.Name.Trim();

        ruleSet.FullTimeWeeklyHours = dto.FullTimeWeeklyHours;

        ruleSet.StatutoryAnnualDays = dto.StatutoryAnnualDays;
        ruleSet.StatutoryEarningStartMonth = dto.StatutoryEarningStartMonth;
        ruleSet.StatutoryEarningStartDay = dto.StatutoryEarningStartDay;
        ruleSet.StatutoryUsageEndMonth = dto.StatutoryUsageEndMonth;
        ruleSet.StatutoryUsageEndDay = dto.StatutoryUsageEndDay;

        ruleSet.MainHolidayDays = dto.MainHolidayDays;
        ruleSet.MainHolidayStartMonth = dto.MainHolidayStartMonth;
        ruleSet.MainHolidayStartDay = dto.MainHolidayStartDay;
        ruleSet.MainHolidayEndMonth = dto.MainHolidayEndMonth;
        ruleSet.MainHolidayEndDay = dto.MainHolidayEndDay;

        ruleSet.MainHolidayNoticeMonths = dto.MainHolidayNoticeMonths;
        ruleSet.OtherHolidayNoticeMonths = dto.OtherHolidayNoticeMonths;

        ruleSet.SixthWeekAnnualHours = dto.SixthWeekAnnualHours;

        ruleSet.SixthWeekEarningStartMonth = dto.SixthWeekEarningStartMonth;
        ruleSet.SixthWeekEarningStartDay = dto.SixthWeekEarningStartDay;

        ruleSet.SixthWeekUsageStartMonth = dto.SixthWeekUsageStartMonth;
        ruleSet.SixthWeekUsageStartDay = dto.SixthWeekUsageStartDay;

        ruleSet.SixthWeekUsageEndMonth = dto.SixthWeekUsageEndMonth;
        ruleSet.SixthWeekUsageEndDay = dto.SixthWeekUsageEndDay;

        ruleSet.MaxStatutoryDebtDays = dto.MaxStatutoryDebtDays;

        ruleSet.FifthWeekTransferDeadlineMonth = dto.FifthWeekTransferDeadlineMonth;
        ruleSet.FifthWeekTransferDeadlineDay = dto.FifthWeekTransferDeadlineDay;

        ruleSet.SixthWeekTransferDeadlineMonth = dto.SixthWeekTransferDeadlineMonth;
        ruleSet.SixthWeekTransferDeadlineDay = dto.SixthWeekTransferDeadlineDay;
    }

    public GlobalRuleSetDto UpdateRuleSet(
        string id,
        UpdateGlobalRuleSetRequestDto dto)
    {
        var ruleSet = db.GlobalRuleSets
            .FirstOrDefault(r => r.GlobalRuleSetId == id);

        if (ruleSet is null)
            throw new ValidationException("Rule set not found");
        
        EnsureScheduled(ruleSet);
        ApplyUpdates(ruleSet, dto);
        ValidateRuleSet(ruleSet);

        db.Update(ruleSet);
        
        return new GlobalRuleSetDto(ruleSet);
    }

    public void DeleteRuleSet(string id)
    {
        var ruleSets = db.GlobalRuleSets
            .OrderBy(r => r.ValidFrom)
            .ToList();
        
        var index = ruleSets.FindIndex(
            r => r.GlobalRuleSetId == id);
        
        if (index == -1)
            throw new ValidationException("Rule set not found");
        
        var ruleSet = ruleSets[index];
        
        EnsureScheduled(ruleSet);
        
        if (index == 0)
            throw new ValidationException("The initial rule set cannot be deleted");
        
        var previous = ruleSets[index - 1];
        
        GlobalRuleSet? next = 
            index < ruleSets.Count - 1 
                ? ruleSets[index + 1] 
                : null;

        using var transaction = db.BeginTransaction();
        
        previous.ValidUntil = next is null
            ? null
            : next.ValidFrom.AddDays(-1);

        db.Update(previous);
        db.Delete(ruleSet);

        transaction.Commit();
    }
}