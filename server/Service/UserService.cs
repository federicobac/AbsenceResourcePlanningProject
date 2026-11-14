using Infra;
using Service.RequestDtos;
using Service.Security;

namespace Service;

public class UserService(
    MyDatabaseConnection db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    public LoginResponseDto? Login(LoginRequestDto loginRequestDto)
    {
        var user = db.Users
            .FirstOrDefault(u => u.Username == loginRequestDto.Username);
        
        if (user == null)
            return null;

        bool passwordIsValid = passwordHasher.VerifyHashedPassword(
            loginRequestDto.Password,
            user.PasswordHash);
        
        if (!passwordIsValid)
            return null;

        return new LoginResponseDto
        {
            Token = tokenService.CreateToken(user),
            User = new UserDto(user)
        };
    }

    public UserDto? GetById(string userId)
    {
        var user = db.Users
            .FirstOrDefault(u => u.UserId == userId);
        
        return user is null
            ? null
            : new UserDto(user);
    }
}