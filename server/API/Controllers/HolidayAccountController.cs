using System.Security.Claims;
using Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.RequestDtos;
using Service.Security;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidayAccountController(
    HolidayAccountService service,
    HolidayDeductionService deductionService,
    HolidayAccrualService accrualService) : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<List<HolidayAccount>> GetHolidayAccounts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();
        
        return Ok(service.GetAccounts(userId));
    }

    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Accountant)]
    [HttpGet("user/{userId}")]
    public List<HolidayAccountDto> GetEmployeeAccounts(string userId)
    {
        return service.GetAccounts(userId);
    }
    
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Accountant)]
    [HttpPost("deduction-preview/user/{userId}")]
    public HolidayDeductionPreviewDto PreviewDeduction(
        string userId,
        PreviewHolidayDeductionRequestDto dto)
    {
        return deductionService.PreviewDeduction(
            userId,
            dto.StartDate,
            dto.EndDate);
    }
    
    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("accrual/user/{userId}/{monthStart}")]
    public List<HolidayAccountEntryDto> PostMonthlyAccrual(
        string userId,
        DateOnly monthStart)
    {
        return accrualService.PostCompletedMonth(userId, monthStart);
    }
}