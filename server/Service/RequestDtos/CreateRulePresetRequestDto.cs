using Facet;
using Infra;

namespace Service.RequestDtos;

[Facet(sourceType: typeof(RulePresetPeriod),
    exclude:
    [
        nameof(RulePresetPeriod.RulePresetPeriodId),
        nameof(RulePresetPeriod.ValidUntil)
    ])]
public partial class CreateRulePresetPeriodRequestDto;