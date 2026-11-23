using Facet;
using Infra;

namespace Service.RequestDtos;

[Facet(
    sourceType: typeof(GlobalRuleSet),
    exclude:
    [
    nameof(GlobalRuleSet.GlobalRuleSetId),
    nameof(GlobalRuleSet.ValidUntil)
    ])]
public partial class CreateGlobalRuleSetRequestDto;