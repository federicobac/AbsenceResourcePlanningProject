using LinqToDB.Mapping;

namespace Infra;

public class HolidayRequest
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    
    [PrimaryKey]
    public string HolidayRequestId { get; set; }
    
    public string UserId { get; set; }
    
    public string AccountType { get; set; }
    
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    
    public decimal RequestedHours { get; set; }
    
    public string Status { get; set; }
    
    public string? Comment  { get; set; }
    
    public DateTime CreatedAtUtc  { get; set; }
    
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewComment { get; set; }
    
}