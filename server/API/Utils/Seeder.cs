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
        db.CreateTable<GlobalRuleSet>(tableOptions: TableOptions.CreateIfNotExists);
        db.CreateTable<RulePreset>(tableOptions: TableOptions.CreateIfNotExists);
        db.CreateTable<RulePresetPeriod>(tableOptions: TableOptions.CreateIfNotExists);
        
        // Seed Global Rule Sets
        if (!db.GlobalRuleSets.Any())
        {
            db.Insert(new GlobalRuleSet
            {
                GlobalRuleSetId = Guid.NewGuid().ToString(),

                Name = "Global Rule Set Default",

                ValidFrom = new DateOnly(2026, 1, 1),
                ValidUntil = null,

                // General
                FullTimeWeeklyHours = 37m,

                // Statutory holiday: weeks 1–5
                StatutoryAnnualDays = 25m,

                StatutoryEarningStartMonth = 9,
                StatutoryEarningStartDay = 1,

                StatutoryUsageEndMonth = 12,
                StatutoryUsageEndDay = 31,

                // Main holiday
                MainHolidayDays = 15,

                MainHolidayStartMonth = 5,
                MainHolidayStartDay = 1,

                MainHolidayEndMonth = 9,
                MainHolidayEndDay = 30,

                // Notice periods
                MainHolidayNoticeMonths = 3,
                OtherHolidayNoticeMonths = 1,

                // Sixth week
                SixthWeekEnabled = true,
                SixthWeekAnnualHours = 37m,

                SixthWeekEarningStartMonth = 1,
                SixthWeekEarningStartDay = 1,

                SixthWeekUsageStartMonth = 5,
                SixthWeekUsageStartDay = 1,

                SixthWeekUsageEndMonth = 4,
                SixthWeekUsageEndDay = 30,

                // Holiday debt
                MaxStatutoryDebtDays = 5,

                // Transfer deadlines
                FifthWeekTransferDeadlineMonth = 12,
                FifthWeekTransferDeadlineDay = 31,

                SixthWeekTransferDeadlineMonth = 9,
                SixthWeekTransferDeadlineDay = 30
            });
        }

        // Seed the two Rule Presets / Profiles
        if (!db.RulePresets.Any(p => p.RulePresetCode == RulePreset.Minimum))
        {
            db.Insert(new RulePreset
            {
                RulePresetCode = RulePreset.Minimum,
                Name = "Ferieloven (minimum)"
            });
        }

        if (!db.RulePresets.Any(p => p.RulePresetCode == RulePreset.KlSixthWeek))
        {
            db.Insert(new RulePreset
            {
                RulePresetCode = RulePreset.KlSixthWeek,
                Name = "Ferieloven + 6. ferieuge (KL 05.11 model)"
            });
        }

        // Seed Rule Preset Period = KL SIXTH WEEK
        if (!db.RulePresetPeriods.Any())
        {
            db.Insert(new RulePresetPeriod
            {
                RulePresetPeriodId = Guid.NewGuid().ToString(),
                RulePresetCode = RulePreset.KlSixthWeek,
                ValidFrom = new DateOnly(2026, 1, 1),
                ValidUntil = null
            });
        }

        // Seed Users (+ working schedule for employee)
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
                FridayTo = new TimeOnly(15, 00),
            });
            
            transaction.Commit();
        }
    }
}