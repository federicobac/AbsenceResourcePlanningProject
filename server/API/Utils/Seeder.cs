using Infra;
using LinqToDB;
using Service.Security;

namespace API.Utils;

public class Seeder(
    MyDatabaseConnection db,
    IPasswordHasher passwordHasher,
    IConfiguration configuration)
{
    public void Seed()
    {
        db.CreateTable<User>(tableOptions: TableOptions.CreateIfNotExists);
        db.CreateTable<HolidayDate>(tableOptions: TableOptions.CreateIfNotExists);
        db.CreateTable<WorkingSchedule>(tableOptions: TableOptions.CreateIfNotExists);

        var adminPassword = configuration["SeedAdmin:Password"];
        var adminEmail = configuration["SeedAdmin:Email"];

        if (!string.IsNullOrEmpty(adminPassword) &&
            !string.IsNullOrEmpty(adminEmail) &&
            !db.Users.Any(u => u.Role == UserRoles.Admin))
        {
            db.Insert(new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = "admin",
                Email = adminEmail,
                PasswordHash =
                    passwordHasher.HashAndSaltPassword(adminPassword),
                Role = UserRoles.Admin
            });
        }
        
        var accountantPassword = configuration["SeedAccountant:Password"];
        var accountantEmail = configuration["SeedAccountant:Email"];
        
        if (!string.IsNullOrEmpty(accountantPassword) &&
            !string.IsNullOrEmpty(accountantEmail) &&
            !db.Users.Any(u => u.Role == UserRoles.Accountant))
        {
            db.Insert(new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = "accountant",
                Email = accountantEmail,
                PasswordHash =
                    passwordHasher.HashAndSaltPassword(accountantPassword),
                Role = UserRoles.Accountant
            });
        }
        
        var employeePassword = configuration["SeedEmployee:Password"];
        var employeeEmail = configuration["SeedEmployee:Email"];

        if (!string.IsNullOrEmpty(employeePassword) &&
            !string.IsNullOrEmpty(employeePassword) &&
            !db.Users.Any(u => u.Role == UserRoles.Employee))
        {
            db.Insert(new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = "employee",
                Email = employeeEmail,
                PasswordHash =
                    passwordHasher.HashAndSaltPassword(employeePassword),
                Role = UserRoles.Employee
            });
        }
    }
}