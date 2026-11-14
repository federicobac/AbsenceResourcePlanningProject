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

        var accountantPassword = configuration["SeedAccountant:Password"];

        if (!string.IsNullOrEmpty(accountantPassword) &&
            !db.Users.Any(u => u.Username == "accountant"))
        {
            db.Insert(new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = "accountant",
                PasswordHash =
                    passwordHasher.HashAndSaltPassword(accountantPassword),
                Role = UserRoles.Accountant
            });
        }



        var employeePassword = configuration["SeedEmployee:Password"];

        if (!string.IsNullOrEmpty(employeePassword) &&
            !db.Users.Any(u => u.Username == "employee"))
        {
            db.Insert(new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = "employee",
                PasswordHash =
                    passwordHasher.HashAndSaltPassword(employeePassword),
                Role = UserRoles.Employee
            });
        }
    }
}