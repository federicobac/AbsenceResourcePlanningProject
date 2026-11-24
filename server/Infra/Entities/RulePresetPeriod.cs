using LinqToDB.Mapping;

namespace Infra;

public class RulePresetPeriod
{
    [PrimaryKey] public string RulePresetPeriodId { get; set; }
    public string RulePresetCode { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
}