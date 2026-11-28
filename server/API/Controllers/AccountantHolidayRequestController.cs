using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.RequestDtos;
using Service.Security;

namespace API.Controllers;

[ApiController]
[Route("api/HolidayRequests/review")]
[Authorize(Roles = UserRoles.Accountant)]
public class AccountantHolidayRequestController(HolidayRequestService service) : ControllerBase
{
    [HttpGet("pending")]
    public ActionResult<List<HolidayRequestDto>> GetPendingRequest()
    {
        return Ok(service.GetPendingRequest());
    }

    [HttpPost("{requestId}/reject")]
    public ActionResult<HolidayRequestDto> RejectRequest(string requestId, RejectHolidayRequestRequestDto dto)
    {
        var accountantId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (accountantId is null)
            return Unauthorized();

        return Ok(service.RejectRequest(
            requestId,
            accountantId,
            dto));
    }
}