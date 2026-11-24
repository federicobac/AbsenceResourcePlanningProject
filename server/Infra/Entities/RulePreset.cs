using LinqToDB.Mapping;

namespace Infra;

public class RulePreset
{
    public const string Minimum = "FERIELOVEN";
    public const string KlSixthWeek = "KL_SIXTH_WEEK";
    
    [PrimaryKey] public string RulePresetCode { get; set; }
    public string Name { get; set; }
}