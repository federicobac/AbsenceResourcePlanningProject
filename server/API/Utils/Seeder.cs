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
            !string.IsNullOrEmpty(employeeEmail) &&
            !db.Users.Any(u => u.Role == UserRoles.Employee))
        {
            var employee = new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = "employee",
                Email = employeeEmail,
                PasswordHash =
                    passwordHasher.HashAndSaltPassword(employeePassword),
                Role = UserRoles.Employee
            };
            using var transaction = db.BeginTransaction();
            db.Insert(employee);
            db.Insert(new WorkingSchedule
            {
                WorkingScheduleId = Guid.NewGuid().ToString(),
                UserId = employee.UserId,

                ValidFrom = new DateOnly(2026, 1, 1),
                ValidUntil = null,
                
                EmploymentType = "FullTime",

                MondayFrom = new TimeOnly(8, 0),
                MondayTo = new TimeOnly(15, 30),

                TuesdayFrom = new TimeOnly(8, 0),
                TuesdayTo = new TimeOnly(15, 30),

                WednesdayFrom = new TimeOnly(8, 0),
                WednesdayTo = new TimeOnly(15, 30),

                ThursdayFrom = new TimeOnly(8, 0),
                ThursdayTo = new TimeOnly(15, 30),

                FridayFrom = new TimeOnly(8, 0),
                FridayTo = new TimeOnly(15, 0)
            });
            transaction.Commit();
        }
    }
}