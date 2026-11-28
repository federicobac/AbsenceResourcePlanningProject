using System.ComponentModel.DataAnnotations;
using Infra;
using LinqToDB;
using Service.RequestDtos;
using Service.Security;

namespace Service;

public class HolidayRequestService(
    MyDatabaseConnection db,
    HolidayDeductionService deductionService,
    HolidayBalanceProjectionService projectionService,
    GlobalRuleSetService globalRuleSetService,
    WorkingScheduleService workingScheduleService,
    HolidayDateService holidayDateService)
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
        
        // Search upcoming scheduled working days
        for (int i = 0; i < 366 && countedDays < maxDays; i++)
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
            throw new ValidationException("Cannot determine the permitted holiday " +
                                          "debt from the working schedule");
        
        return totalHours;
    }

    // PlannedHolidayDay represents one chargeable holiday date
    private record PlannedHolidayDay(
        DateOnly Date,
        decimal Hours,
        bool IsNewRequest);

    // SimulatedDeduction records which earning-period account would fund a particular day
    private record SimulatedDeduction(
        DateOnly Date,
        DateOnly EarningPeriodStart,
        decimal Hours);

    public HolidayFundingPreviewDto PreviewFunding(
        string userId,
        CreateHolidayRequestRequestDto dto)
    {
        // Reuse our existing validation and deduction calculation
        var deduction = PreviewRequest(userId, dto);

        var plannedDays = deduction.Days
            .Select(d => new PlannedHolidayDay(
                d.Date,
                d.Hours,
                true))
            .ToList();

        // Determine how far we need to project
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
                    .FirstOrDefault(a =>
                        a.EarningPeriodStart <= day.Date &&
                        a.EarningPeriodEnd >= day.Date);

                if (earningAccount is null)
                    throw new ValidationException(
                        $"No statutory earning account found for {day.Date}");

                if (earningAccount.EarningPeriodEnd > horizon)
                    horizon = earningAccount.EarningPeriodEnd;
            }
        }

        // Include existing approved requests
        var approvedRequests = db.HolidayRequests
            .Where(r =>
                r.UserId == userId &&
                r.AccountType == dto.AccountType &&
                r.Status == HolidayRequest.Approved &&
                r.StartDate <= horizon)
            .ToList();

        foreach (var request in approvedRequests)
        {
            // Approved requests already posted to the ledger
            // are included by HolidayBalanceProjectionService
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
                    .Where(d => d.Date <= horizon)
                    .Select(d => new PlannedHolidayDay(
                        d.Date,
                        d.Hours,
                        false)));
        }

        // Keep simulated deductions separate from persisted ledger entries
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

        // Process all chargeable dates chronologically
        foreach (var day in plannedDays.OrderBy(d => d.Date))
        {
            var projection = projectionService.GetProjectedBalance(
                userId,
                dto.AccountType,
                day.Date);

            decimal remaining = day.Hours;

            // Consume eligible entitlement, earliest expiry first
            foreach (var account in projection.Accounts
                         .OrderBy(a => a.UsagePeriodEnd))
            {
                decimal available = account.BalanceHours -
                                    Spent(account.EarningPeriodStart, day.Date);

                if (available <= 0)
                    continue;

                decimal consumed = Math.Min(remaining, available);

                allocations.Add(new SimulatedDeduction(
                    day.Date,
                    account.EarningPeriodStart,
                    consumed));

                remaining -= consumed;

                if (remaining == 0)
                    break;
            }

            if (remaining <= 0)
                continue;

            // Sixth-week holiday cannot use debt
            if (dto.AccountType == HolidayAccount.SixthWeek)
                throw new ValidationException(
                    $"Insufficient sixth-week holiday hours on {day.Date}");

            // Debt must belong to the earning period active on this date
            var currentAccount = projection.Accounts
                .FirstOrDefault(a =>
                    a.EarningPeriodStart <= day.Date &&
                    a.EarningPeriodEnd >= day.Date);

            if (currentAccount is null)
                throw new ValidationException(
                    $"No statutory earning account found for {day.Date}");

            allocations.Add(new SimulatedDeduction(
                day.Date,
                currentAccount.EarningPeriodStart,
                remaining));

            decimal balanceAfterDebt = currentAccount.BalanceHours - Spent(currentAccount.EarningPeriodStart, day.Date);

            decimal currentDebt = Math.Max(0m, -balanceAfterDebt);

            decimal permittedDebt = GetDebtLimitHours(userId, day.Date);

            if (currentDebt > permittedDebt)
                throw new ValidationException($"Statutory holiday debt exceeds the permitted limit on {day.Date}");

            requiresDebt = true;

            if (currentDebt > peakDebtHours)
            {
                peakDebtHours = currentDebt;
                debtLimitHours = permittedDebt;
            }

            recoveryPeriods[currentAccount.EarningPeriodStart] =
                currentAccount.EarningPeriodEnd;
        }

        // Check that all simulated debt is recovered by
        // the end of its corresponding statutory earning period.
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
                throw new ValidationException($"Statutory holiday debt must be repaid by {recoveryDate}");

            recoveries.Add(new HolidayDebtRecoveryDto(
                recoveryDate,
                recoveryBalance));
        }

        // Recalculate the balance at the end of the requested holiday.
        var finalProjection = projectionService.GetProjectedBalance(
            userId,
            dto.AccountType,
            dto.EndDate);

        decimal finalBalance = finalProjection.Accounts.Sum(a =>
            a.BalanceHours - Spent(a.EarningPeriodStart, dto.EndDate));

        return new HolidayFundingPreviewDto(
            deduction,
            finalBalance,
            requiresDebt,
            peakDebtHours,
            debtLimitHours,
            recoveries);
    }

    public HolidayRequestDto CreateRequest(
        string userId,
        CreateHolidayRequestRequestDto dto)
    {
        using var transaction = db.BeginTransaction();
        
        // Run all existing validations
        var funding = PreviewFunding(userId, dto);

        var request = new HolidayRequest
        {
            HolidayRequestId = Guid.NewGuid().ToString(),
            UserId = userId,
            AccountType = dto.AccountType,

            StartDate = dto.StartDate,
            EndDate = dto.EndDate,

            RequestedHours = funding.Deduction.TotalHours,
            Status = HolidayRequest.Pending,
            Comment = dto.Comment,
            CreatedAtUtc = DateTime.UtcNow,

            ReviewedByUserId = null,
            ReviewedAtUtc = null,
            ReviewComment = null
        };
        
        db.Insert(request);
        
        transaction.Commit();
        
        return new HolidayRequestDto(request);
    }

    public List<HolidayRequestDto> GetMyRequests(string userId)
    {
        if (!db.Users.Any(u =>
                u.UserId == userId &&
                u.Role == UserRoles.Employee))
        { throw new ValidationException("Employee not found"); }

        return db.HolidayRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.StartDate)
            .ToList()
            .Select(r => new HolidayRequestDto(r))
            .ToList();
    }

    public List<HolidayRequestDto> GetPendingRequests()
    {
        return db.HolidayRequests
            .Where(r => r.Status == HolidayRequest.Pending)
            .OrderBy(r => r.CreatedAtUtc)
            .ToList()
            .Select(r => new HolidayRequestDto(r))
            .ToList();
    }

    public HolidayRequestDto RejectRequest(
        string requestId,
        string accountantId,
        RejectHolidayRequestRequestDto dto)
    {
        using var transaction = db.BeginTransaction();
        
        // Validate Account
        if (!db.Users.Any(u =>
                u.UserId == accountantId &&
                u.Role == UserRoles.Accountant))
        {
            throw new ValidationException("Accountant not found");
        }
        
        // Retrieve the holiday request
        var request = db.HolidayRequests
            .FirstOrDefault(r => r.HolidayRequestId == requestId);

        if (request is null)
            throw new ValidationException("Holiday request not found");
        
        // Only pending requests can be retrieved
        if (request.Status != HolidayRequest.Pending)
            throw new ValidationException("Only pending holicay requests can be rejected");
        
        // Record the rejection
        request.Status = HolidayRequest.Rejected;

        request.ReviewedByUserId = accountantId;
        request.ReviewedAtUtc = DateTime.UtcNow;

        request.ReviewComment = dto.ReviewComment?.Trim();
        
        db.Update(request);
        
        transaction.Commit();
        
        return new HolidayRequestDto(request);
    }
}