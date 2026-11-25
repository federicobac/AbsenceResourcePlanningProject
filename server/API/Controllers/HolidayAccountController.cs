using System.Security.Claims;
using Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.Security;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidayAccountController(HolidayAccountService service) : ControllerBase
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
}