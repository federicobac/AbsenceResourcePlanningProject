using System.ComponentModel.DataAnnotations;
using Infra;
using LinqToDB;
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

    public List<UserDto> GetUsers()
    {
        return db.Users.Select(u => new UserDto(u)).ToList();
    }

    public UserDto CreateUser(CreateUserRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
            throw new ValidationException("Username is required");

        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new ValidationException("Password is required");

        if (db.Users.Any(u => u.Username == dto.Username))
            throw new ValidationException("Username already exists");
        
        if (string.IsNullOrWhiteSpace(dto.Role))
            throw new ValidationException("Role is required");
        

        string passwordHash =
            passwordHasher.HashAndSaltPassword(dto.Password);
        var user = new User()
        {
            UserId = Guid.NewGuid().ToString(),
            Username = dto.Username,
            PasswordHash = passwordHash,
            Role = dto.Role
            //role should be dynamic (shall we inject it? or fetch/alike in case of dropbox/ string or value? )
            // Design TBD still...not sure if we gonna use this just yet...fix maybe

        };
        db.Insert(user);

        return new UserDto(user);
    }
    public UserDto UpdateUser(string id, UpdateUserRequestDto dto)
    {
        if  (string.IsNullOrWhiteSpace(dto.Username))
            throw new ValidationException("Username is required");
        
        var user = db.Users.FirstOrDefault(u => u.UserId == id);
        
        if (user is null)
            throw new ValidationException("User not found");
        user.Username = dto.Username;
        db.Update(user);
        return new UserDto(user);
        
        //TBD redo update logic
        /* PAssword validation and
        (it shoulduldt be shown /"empty imput field by default"
        if empty field , dont update it, if new value/target.value update it ) */
        //role validation tbd
    }

    public void DeleteUser(string id)
    {
        var user = db.Users.FirstOrDefault(u => u.UserId == id);
        if (user is null)
            throw new ValidationException("User not found");
        db.Delete(user);       
    }
}

//PD figure the workflow for users creation + update (can roles be updated? and the validations for such)