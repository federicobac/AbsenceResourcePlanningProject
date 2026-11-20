using LinqToDB.Mapping;

namespace Infra;

public class HolidayDate
{
    [PrimaryKey] public string HolidayDateId { get; set; }
    
    public DateOnly Date { get; set; }
    
    public string Name { get; set; }
    
    public string Type { get; set; }
}