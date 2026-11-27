using System.ComponentModel.DataAnnotations;
using Infra;

namespace Service;

public class HolidayBalanceProjectionService(
    MyDatabaseConnection db,
    HolidayAccountService holidayAccountService,
    HolidayAccrualService holidayAccrualService)
{
    public ProjectedHolidayBalanceDto GetProjectedBalance(
        string userId,
        string accountType,
        DateOnly date)
    {
        if (date == default)
            throw new ValidationException("Projection date is required.");

        if (accountType is not (
            HolidayAccount.Statutory or HolidayAccount.SixthWeek))
            throw new ValidationException("Invalid holiday account type");

        var accounts = holidayAccountService
            .GetProjectionAcconts(userId, date)
            .Where(a => a.AccountType == accountType)
            .OrderBy(a => a.UsagePeriodEnd)
            .ToList();

        var results = new List<ProjectedHolidayAccountDto>();

        foreach (var account in accounts)
        {
            decimal earnedHours =
                holidayAccrualService.CalculateAccruedHours(
                    userId,
                    account,
                    date);

            decimal otherEntries = 0m;

            if (!string.IsNullOrEmpty(account.HolidayAccountId))
            {
                otherEntries = db.HolidayAccountEntries
                    .Where(e =>
                        e.HolidayAccountId == account.HolidayAccountId &&
                        e.EntryType != "ACCRUAL" &&
                        e.EffectiveDate <= date)
                    .Select(e => e.Hours)
                    .ToList()
                    .Sum();
            }

            decimal balance = earnedHours + otherEntries;
                
                results.Add(new ProjectedHolidayAccountDto(
                    account.AccountType,
                    account.EarningPeriodStart,
                    account.EarningPeriodEnd,
                    account.UsagePeriodEnd,
                    earnedHours,
                    otherEntries,
                    balance));

                
            }
            return new ProjectedHolidayBalanceDto(
                userId,
                accountType,
                date,
                results.Sum(a => a.BalanceHours),
                results);
        }

    }
