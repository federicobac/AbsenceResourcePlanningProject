using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.RequestDtos;
using Service.Security;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = UserRoles.Admin)]
public class GlobalRuleSetController(GlobalRuleSetService service) : ControllerBase
{
    [HttpGet]
    public List<GlobalRuleSetDto> GetRuleSets()
    {
        return service.GetRuleSets();
    }

    [HttpGet("applicable/{date}")]
    public GlobalRuleSetDto GetApplicableRuleSet(DateOnly date)
    {
        return service.GetApplicableRulesSet(date);
    }

    [HttpPost]
    public GlobalRuleSetDto CreateRuleSet(CreateGlobalRuleSetRequestDto dto)
    {
        return service.CreateRuleSet(dto);
    }

    [HttpPut("{id}")]
    public GlobalRuleSetDto UpdateRuleSet(string id, UpdateGlobalRuleSetRequestDto dto)
    {
        return service.UpdateRuleSet(id, dto);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteRuleSet(string id)
    {
        service.DeleteRuleSet(id);
        return NoContent();
    }
}