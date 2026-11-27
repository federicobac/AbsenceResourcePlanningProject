using Facet;
using Infra;

namespace Service;

[Facet(sourceType:typeof(User),exclude:[nameof(User.PasswordHash)])]
public partial class UserDto;

[Facet(sourceType:typeof(HolidayDate))]
public partial class HolidayDateDto;

[Facet(sourceType:typeof(WorkingSchedule))]
public partial class WorkingScheduleDto;

[Facet(sourceType:typeof(GlobalRuleSet))]
public partial class GlobalRuleSetDto;

[Facet(sourceType:typeof(RulePreset))]
public partial class RulePresetDto;
[Facet(sourceType:typeof(RulePresetPeriod))]
public partial class RulePresetPeriodDto;

[Facet(sourceType: typeof(HolidayAccount))]
public partial class HolidayAccountDto;

[Facet(sourceType: typeof(HolidayAccountEntry))]
public partial class HolidayAccountEntryDto;

public record HolidayDeductionDayDto(
    DateOnly Date,
    decimal Hours);

public record HolidayDeductionPreviewDto(
    string UserId,
    DateOnly StartDate,
    DateOnly EndDate,
    int WorkingDays,
    decimal TotalHours,
    List<HolidayDeductionDayDto> Days);

[Facet(sourceType:typeof(HolidayRequest))]
public partial class HolidayRequestDto;

public record ProjectedHolidayAccountDto(
    string AccountType,
    DateOnly EarningPeriodStart,
    DateOnly EarningPeriodEnd,
    DateOnly UsagePeriodEnd,
    decimal EarnedHours,
    decimal OtherEntriesHours,
    decimal BalanceHours
);

public record ProjectedHolidayBalanceDto(
    string UserId,
    string AccountType,
    DateOnly AsOfDate,
    decimal TotalHours,
    List<ProjectedHolidayAccountDto> Accounts);

public record HolidayDebtRecoveryDto(
    DateOnly RecoveryDeadline,
    decimal ProjectedBalanceHours);

public record HolidayFundingPreviewDto(
    HolidayDeductionPreviewDto Deduction,
    decimal BalanceAfterRequestHours,
    bool RequiresDebt,
    decimal PeakDebtHours,
    decimal DebtLimitHours,
    List<HolidayDebtRecoveryDto> Recoveries);

