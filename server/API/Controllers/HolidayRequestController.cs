using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.RequestDtos;
using Service.Security;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = UserRoles.Employee)]
public class HolidayRequestController( 
    HolidayRequestService service,
    HolidayBalanceProjectionService projectionService) : ControllerBase
{
    [HttpPost("preview")]
    public ActionResult<HolidayDeductionPreviewDto> PreviewRequest(
        CreateHolidayRequestRequestDto dto)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        return Ok(service.PreviewRequest(userId, dto));
    }

    [HttpGet("balance-preview/{accountType}/{date}")]
    public ActionResult<ProjectedHolidayBalanceDto> GetProjectedBalance(string accountType, DateOnly date)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();
        
        return Ok(projectionService.GetProjectedBalance(
            userId,
            accountType,
            date));
    }

    [HttpPost("funding-preview")]
    public ActionResult<HolidayFundingPreviewDto> PreviewFunding(CreateHolidayRequestRequestDto dto)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        return Ok(service.PreviewFunding(userId, dto));
    }
}