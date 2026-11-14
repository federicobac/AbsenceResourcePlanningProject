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
        
        //Seed Admin
        var adminPassword = configuration["SeedAdmin:Password"];

        if (!string.IsNullOrWhiteSpace(adminPassword) &&
            !db.Users.Any(u => u.Username == "admin"))
        {
            db.Insert(new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = "admin",
                PasswordHash = passwordHasher.HashAndSaltPassword(adminPassword),
                Role = UserRoles.Admin
            });
        }
        
        //Seed Accountant
        var accountantPassword = configuration["SeedAccountant:Password"];

        if (!string.IsNullOrWhiteSpace(accountantPassword) &&
            !db.Users.Any(u => u.Username == "accountant"))
        {
            db.Insert(new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = "accountant",
                PasswordHash = passwordHasher.HashAndSaltPassword(accountantPassword),
                Role = UserRoles.Accountant
            });
        }
        
        //Seed Employee
        var employeePassword = configuration["SeedEmployee:Password"];

        if (!string.IsNullOrWhiteSpace(employeePassword) &&
            !db.Users.Any(u => u.Username == "employee"))
        {
            db.Insert(new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = "employee",
                PasswordHash = passwordHasher.HashAndSaltPassword(employeePassword),
                Role = UserRoles.Employee
            });
        }
    }
}