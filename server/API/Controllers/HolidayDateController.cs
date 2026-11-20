using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.RequestDtos;
using Service.Security;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidayDateController(HolidayDateService service) : ControllerBase
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

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("company")]
    public HolidayDateDto CreateCompanyHoliday(CreateCompanyHolidayRequestDto dto)
    {
        return service.CreateCompanyHoliday(dto);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("company/{id}")]
    public HolidayDateDto UpdateCompanyHoliday(
        string id,
        UpdateCompanyHolidayRequestDto dto)
    {
        return service.UpdateCompanyHoliday(id, dto);
    }
    
    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("company/{id}")]
    public IActionResult DeleteCompanyHoliday(string id)
    {
        service.DeleteCompanyHoliday(id);
        return NoContent();
    }
}