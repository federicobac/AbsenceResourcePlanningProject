using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
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
        return db.Users
            .Select(u => new UserDto(u))
            .ToList();
    }

    public UserDto CreateUser(CreateUserRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
            throw new ValidationException("Username is required");
        
        string email = ValidateEmail(dto.Email);

        if (db.Users.Any(u => u.Email != null && u.Email.ToLower() == email))
            throw new ValidationException("Email already exists");

        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new ValidationException("Password is required");

        if (string.IsNullOrWhiteSpace(dto.Role))
            throw new ValidationException("Role is required");

        if (db.Users.Any(u => u.Username == dto.Username))
            throw new ValidationException("Username already exists");

        string passwordHash = passwordHasher.HashAndSaltPassword(dto.Password);

        var user = new User()
        {
            UserId = Guid.NewGuid().ToString(),
            Username = dto.Username,
            Email = email,
            PasswordHash = passwordHash,
            Role = dto.Role
        };
        db.Insert(user);
        return new UserDto(user);
    }

    public UserDto UpdateUser(
        string id,
        UpdateUserRequestDto dto)
    {
        var user = db.Users
            .FirstOrDefault(u => u.UserId == id);

        if (user is null)
            throw new ValidationException("User not found");

        if (string.IsNullOrWhiteSpace(dto.Username))
            throw new ValidationException("Username is required");

        if (db.Users.Any(u => u.Username == dto.Username && u.UserId != id))
            throw new ValidationException("Username already exists");

        user.Username = dto.Username;
        user.Role = dto.Role;

        string email = ValidateEmail(dto.Email);
        
        if (db.Users.Any(u => 
                u.Email != null && 
                u.Email.ToLower() == email &&
                u.UserId != id))
            throw new ValidationException("Email already exists");
        
        // Only update password if a new one was provided
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = passwordHasher.HashAndSaltPassword(dto.Password);
        }

        db.Update(user);
        return new UserDto(user);
    }

    public void DeleteUser(string id)
    {
        var user = db.Users
            .FirstOrDefault(u => u.UserId == id);

        if (user is null)
            throw new ValidationException("User not found");

        db.Delete(user);
    }

    private static string ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Email is required");

        email = email.Trim();

        try
        {
            var address = new MailAddress(email);

            if (!address.Address.Equals(email, StringComparison.OrdinalIgnoreCase) ||
                !address.Host.Equals("businessesbjerg.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(
                    "Email must belong to @businessesbjerg.com");
            }
        }
        catch (FormatException)
        {
            throw new ValidationException("Invalid email format");
        }

        return email.ToLowerInvariant();
    }
}