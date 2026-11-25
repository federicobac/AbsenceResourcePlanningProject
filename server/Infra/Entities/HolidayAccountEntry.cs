using LinqToDB.Mapping;

namespace Infra;

public class HolidayAccountEntry
{
    [PrimaryKey] public string HolidayAccountEntryId { get; set; }
    public string HolidayAccountId { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string EntryType { get; set; }
    public decimal Hours { get; set; }
    public string? ReferenceId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}