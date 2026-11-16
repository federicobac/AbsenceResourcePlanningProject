using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.RequestDtos;
using Service.Security;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(UserService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponseDto> Login(LoginRequestDto loginRequestDto)
    {
        var result = service.Login(loginRequestDto);
        if (result is null)
            return Unauthorized();
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<UserDto> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();
        
        var user = service.GetById(userId);

        if (user is null)
            return Unauthorized();
        
        return Ok(user);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet(nameof(GetUsers))]
    public List<UserDto> GetUsers()
    {
        return service.GetUsers();
    }


    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost(nameof(CreateUser))]
    public UserDto CreateUser(CreateUserRequestDto dto)
    {
        return service.CreateUser(dto);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id}")]
    public UserDto UpdateUser(
        string id,
        UpdateUserRequestDto dto)
    {
        return service.UpdateUser(id, dto);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id}")]
    public IActionResult DeleteUser(string id)
    {
        service.DeleteUser(id);
        return NoContent();
    }
}