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

[Facet(sourceType:typeof(HolidayAccount))]
public partial class HolidayAccountDto;

[Facet(sourceType:typeof(HolidayAccountEntry))]
public partial class HolidayAccountEntryDto;