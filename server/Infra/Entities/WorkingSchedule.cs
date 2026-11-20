using LinqToDB.Mapping;

namespace Infra;

public class WorkingSchedule
{
    [PrimaryKey]
    public string WorkingScheduleId { get; set; }
    public string UserId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public string EmploymentType { get; set; }
    
    public TimeOnly? MondayFrom { get; set; }
    public TimeOnly? MondayTo { get; set; }
    
    public TimeOnly? TuesdayFrom { get; set; }
    public TimeOnly? TuesdayTo { get; set; }
    
    public TimeOnly? WednesdayFrom { get; set; }
    public TimeOnly? WednesdayTo { get; set; }
    
    public TimeOnly? ThursdayFrom { get; set; }
    public TimeOnly? ThursdayTo { get; set; }
    
    public TimeOnly? FridayFrom { get; set; }
    public TimeOnly? FridayTo { get; set; }
    
}