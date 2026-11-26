using LinqToDB.Mapping;

namespace Infra;

public class HolidayAccount
{
    public const string Statutory = "STATUTORY";
    public const string SixthWeek = "SIXTH_WEEK";
    
    [PrimaryKey]
    public string  HolidayAccountId { get; set; }
    public string UserId { get; set; }
    public string AccountType { get; set; }
    
    public DateOnly EarningPeriodStart { get; set; }
    public DateOnly EarningPeriodEnd { get; set; }
    
    public DateOnly UsagePeriodStart { get; set; }
    public DateOnly UsagePeriodEnd { get; set; }
}