using Facet;
using Infra;

namespace Service.RequestDtos;

[Facet(sourceType: typeof(GlobalRuleSet),
    exclude:
    [
        nameof(GlobalRuleSet.GlobalRuleSetId),
        nameof(GlobalRuleSet.ValidFrom),
        nameof(GlobalRuleSet.ValidUntil)
    ])]
public partial class UpdateGlobalRuleSetRequestDto;