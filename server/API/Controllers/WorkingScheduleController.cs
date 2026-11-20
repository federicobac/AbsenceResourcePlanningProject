using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.RequestDtos;
using Service.Security;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = UserRoles.Admin)]
public class WorkingScheduleController(WorkingScheduleService service) : ControllerBase
{
    [HttpGet("{userId}")]
    public List<WorkingScheduleDto> GetSchedule(string userId)
    {
        return service.GetSchedules(userId);
    }

    [HttpPost("{userId}")]
    public WorkingScheduleDto CreateSchedule(
        string userId,
        CreateWorkingScheduleRequestDto dto)
    {
        return service.CreateSchedule(userId, dto);
    }
}