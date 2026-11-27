using System.ComponentModel.DataAnnotations;
using Infra;
using Service.RequestDtos;
using Service.Security;

namespace Service;

public class HolidayRequestService(
    MyDatabaseConnection db,
    HolidayDeductionService deductionService,
    HolidayBalanceProjectionService projectionService,
    GlobalRuleSetService globalRuleSetService,
    WorkingScheduleService  workingScheduleService,
    HolidayDateService holidayDateService)
{
    private static DateOnly Today()
    {
        var timezone = TimeZoneInfo.FindSystemTimeZoneById(
            "Europe/Copenhagen");

        var danishTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow, timezone);
        
        return DateOnly.FromDateTime(danishTime);
    }

    private static void ValidateAccountType(string accountType)
    {
        if (accountType is not (
            HolidayAccount.Statutory or HolidayAccount.SixthWeek))
        {
            throw new ValidationException("Invalid holiday account type");
        }
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

    public HolidayDeductionPreviewDto PreviewRequest(string userId, CreateHolidayRequestRequestDto dto)
    {
        if (!db.Users.Any(u =>
                u.UserId == userId &&
                u.Role == UserRoles.Employee))
        {
            throw new ValidationException("Employee not found");
        }
        ValidateAccountType(dto.AccountType);
        
        ValidateDates(dto.StartDate, dto.EndDate);
        
        ValidateNoOverlap(
            userId,
            dto.StartDate,
            dto.EndDate);

        return deductionService.PreviewDeduction(
            userId,
            dto.StartDate,
            dto.EndDate);

    }

    private decimal GetDebtLimitHours(string userId, DateOnly date)

    {
        var rules = globalRuleSetService
            .GetApplicableRulesSet(date);

        int maxDays = rules.MaxStatutoryDebtDays;

        if (maxDays == 0)
            return 0m;

        decimal totalHours = 0m;
        int countedDays = 0;
        
        var currentDate = date;

        for (int i = 0; i < 365 && countedDays < maxDays; ++i)
        {
            if (!holidayDateService.IsNonWorkingDay(currentDate))
            {
                decimal hours = workingScheduleService
                    .GetScheduledHoursForDate(userId, currentDate);

                if (hours > 0)
                {
                    totalHours += hours;
                    countedDays++;
                }
            }
            currentDate = currentDate.AddDays(1);
        }
        if (countedDays < maxDays)
            throw new ValidationException("Cannot determine the permitted holiday debt from the working schedule");
        
        return totalHours;
        
    }

    private record PlannedHolidayDay(
        DateOnly Date,
        decimal Hours,
        bool IsNewRequest);

    private record SimulatedDeduction(
        DateOnly Date,
        DateOnly EarningPeriodStart,
        decimal Hours);

    public HolidayFundingPreviewDto PreviewFunding(string userId, CreateHolidayRequestRequestDto dto)
    {
        var deduction = PreviewRequest(userId, dto);

        var plannedDays = deduction.Days
            .Select(d => new PlannedHolidayDay(
                d.Date,
                d.Hours,
                true))
            .ToList();


        DateOnly horizon = dto.EndDate;

        if (dto.AccountType == HolidayAccount.Statutory)
        {
            foreach (var day in deduction.Days)
            {
                var projection = projectionService.GetProjectedBalance(
                    userId,
                    dto.AccountType,
                    day.Date);
                
                var earningAccount = projection.Accounts
                    .FirstOrDefault(a=>
                        a.EarningPeriodStart <=  day.Date && 
                        a.EarningPeriodEnd >= day.Date);

                if (earningAccount is null)
                    throw new ValidationException($"No Statutory earning found for {day.Date}");

                if (earningAccount.EarningPeriodEnd > horizon)
                    horizon = earningAccount.EarningPeriodEnd;
            }
        }
        var approvedrequests = db.HolidayRequests
            .Where(r =>
                r.UserId == userId && 
                r.AccountType == dto.AccountType &&
                r.Status == HolidayRequest.Approved &&
                r.StartDate <= horizon)
            .ToList();

        foreach (var request in approvedrequests)
        {
            bool posted = db.HolidayAccountEntries.Any(e =>
                e.ReferenceId == request.HolidayRequestId &&
                e.EntryType == "DEDUCTION");
            
            if (posted)
                continue;
            
            var approvedDeduction = deductionService.PreviewDeduction(
                userId,
                request.StartDate,
                request.EndDate);
            
            plannedDays.AddRange(
                approvedDeduction.Days
                    .Where(d=> d.Date <= horizon)
                    .Select(d => new PlannedHolidayDay(
                        d.Date,
                        d.Hours,
                        false)));
        }
        var allocations = new List<SimulatedDeduction>();

        decimal Spent(DateOnly earningStart, DateOnly throughDate)
        {
            return allocations
                .Where(a =>
                    a.EarningPeriodStart == earningStart &&
                    a.Date <= throughDate)
                .Sum(a => a.Hours);
        }

        bool requiresDebt = false;
        decimal peakDebtHours = 0m;
        decimal debtLimitHours = 0m;

        var recoveryPeriods = new Dictionary<DateOnly, DateOnly>();

        foreach (var day in plannedDays.OrderBy(d => d.Date))
        {
            var projection = projectionService.GetProjectedBalance(
                userId,
                dto.AccountType,
                day.Date);

            decimal remaining = day.Hours;

            foreach (var account in projection.Accounts
                         .OrderBy(a => a.UsagePeriodEnd))
            {
                decimal avaliable = account.BalanceHours -
                                    Spent(account.EarningPeriodStart, day.Date);
                
                if (avaliable <= 0)
                    continue;
                
                decimal consumed = Math.Min(remaining, avaliable);
                
                allocations.Add(new SimulatedDeduction(
                    day.Date,
                    account.EarningPeriodStart,
                    consumed));
                
                if (remaining == 0)
                    break;
            }
            if (remaining <= 0)
                continue;

            if (dto.AccountType == HolidayAccount.SixthWeek)
                throw new ValidationException($"$Insuficient sixth-week holiday hours on{day.Date}");

            var currentAccount = projection.Accounts
                .FirstOrDefault(a =>
                    a.EarningPeriodStart <= day.Date &&
                    a.EarningPeriodEnd >= day.Date);
            
            if (currentAccount is null)
                throw new ValidationException($"No statutory earning account found for {day.Date}");
            
            allocations.Add( new SimulatedDeduction(
                day.Date,
                currentAccount.EarningPeriodStart,
                remaining));
            
            decimal balanceAfterDebt = currentAccount.BalanceHours -
                                       Spent(currentAccount.EarningPeriodStart, day.Date);
            
            decimal currentDebt = Math.Max(0m, -balanceAfterDebt);

            decimal permittedDebt = GetDebtLimitHours(
                userId,
                day.Date);
            
            if (currentDebt > permittedDebt)
                throw new ValidationException($"Statutory holiday exceeds the permitted limit on {day.Date}");

            requiresDebt = true;
            
            if( currentDebt > peakDebtHours)
            {
                peakDebtHours = currentDebt;
                debtLimitHours = permittedDebt;
            }

            recoveryPeriods[currentAccount.EarningPeriodStart] =
                currentAccount.EarningPeriodEnd;
        }

        var recoveries = new List<HolidayDebtRecoveryDto>();

        foreach (var (earningStart, recoveryDate) in recoveryPeriods)
        {
            var projection = projectionService.GetProjectedBalance(
                userId,
                HolidayAccount.Statutory,
                recoveryDate);

            var account = projection.Accounts
                .FirstOrDefault(a =>
                    a.EarningPeriodStart == earningStart);

            if (account is null)
                throw new ValidationException("Cannot determine the statutory debt recovery balance");
            
            decimal recoveryBalance = account.BalanceHours -
                                      Spent(earningStart, recoveryDate);

            if (recoveryBalance < 0)
                throw new ValidationException($"$Statutory holiday debt must be repaid by {recoveryDate}]");
            
            recoveries.Add(new HolidayDebtRecoveryDto(
                recoveryDate,
                recoveryBalance));
        }

        var finalProjection = projectionService.GetProjectedBalance(
            userId,
            dto.AccountType,
            dto.EndDate);

        decimal finalBalance = finalProjection.Accounts.Sum(a =>
            a.BalanceHours -
            Spent(a.EarningPeriodStart, dto.EndDate));
        
        return new HolidayFundingPreviewDto(
            deduction,
            finalBalance,
            requiresDebt,
            peakDebtHours,
            debtLimitHours,
            recoveries);

    } 

}