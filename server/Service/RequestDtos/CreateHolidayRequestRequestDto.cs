namespace Service.RequestDtos;

public class CreateHolidayRequestRequestDto
{
    public string AccountType { get; set; }
    public DateOnly StartDate  { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Comment { get; set; }
}