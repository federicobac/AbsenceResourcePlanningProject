using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.Security;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidayDateController(
    HolidayDateService service) : ControllerBase
{
    [HttpGet("{year}")]
    public List<HolidayDateDto> GetHolidays(int year)
    {
        return service.GetHolidays(year);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("import/{year}")]
    public async Task<IActionResult> ImportPublicHolidays(int year)
    {
        int imported = await service.ImportPublicHolidays(year);

        return Ok(new { Imported = imported });
    }
}