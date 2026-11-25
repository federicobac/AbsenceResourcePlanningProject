using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.RequestDtos;
using Service.Security;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = UserRoles.Admin)]
public class RulePresetController(
    RulePresetService service) : ControllerBase
{
    [HttpGet]
    public List<RulePresetDto> GetPresets()
    {
        return service.GetPresets();
    }

    [HttpGet("periods")]
    public List<RulePresetPeriodDto> GetPresetPeriods()
    {
        return service.GetPresetPeriods();
    }

    [HttpGet("applicable/{date}")]
    public RulePresetPeriodDto GetApplicablePreset(DateOnly date)
    {
        return service.GetApplicablePreset(date);
    }

    [HttpPost("periods")]
    public RulePresetPeriodDto CreatePresetPeriod(
        CreateRulePresetPeriodRequestDto dto)
    {
        return service.CreatePresetPeriod(dto);
    }

    [HttpPut("periods/{id}")]
    public RulePresetPeriodDto UpdatePresetPeriod(
        string id,
        UpdateRulePresetPeriodRequestDto dto)
    {
        return service.UpdatePresetPeriod(id, dto);
    }

    [HttpDelete("periods/{id}")]
    public IActionResult DeletePresetPeriod(string id)
    {
        service.DeletePresetPeriod(id);

        return NoContent();
    }
}