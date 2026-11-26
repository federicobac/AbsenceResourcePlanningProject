using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.RequestDtos;
using Service.Security;

namespace API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = UserRoles.Employee)]
public class HolidayRequestController( HolidayRequestService service) : ControllerBase
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
}