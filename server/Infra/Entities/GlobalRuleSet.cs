using LinqToDB.Mapping;

namespace Infra;

public class GlobalRuleSet
{
    [PrimaryKey]
    public string GlobalRuleSetId { get; set; }

    public string Name { get; set; }

    // Validity period
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }

    // General
    public decimal FullTimeWeeklyHours { get; set; }

    // Statutory holiday: weeks 1–5
    public decimal StatutoryAnnualDays { get; set; }

    public int StatutoryEarningStartMonth { get; set; }
    public int StatutoryEarningStartDay { get; set; }

    public int StatutoryUsageEndMonth { get; set; }
    public int StatutoryUsageEndDay { get; set; }

    // Main holiday
    public int MainHolidayDays { get; set; }

    public int MainHolidayStartMonth { get; set; }
    public int MainHolidayStartDay { get; set; }

    public int MainHolidayEndMonth { get; set; }
    public int MainHolidayEndDay { get; set; }

    // Notice periods
    public int MainHolidayNoticeMonths { get; set; }
    public int OtherHolidayNoticeMonths { get; set; }

    // Sixth week
    public decimal SixthWeekAnnualHours { get; set; }

    public int SixthWeekEarningStartMonth { get; set; }
    public int SixthWeekEarningStartDay { get; set; }

    public int SixthWeekUsageStartMonth { get; set; }
    public int SixthWeekUsageStartDay { get; set; }

    public int SixthWeekUsageEndMonth { get; set; }
    public int SixthWeekUsageEndDay { get; set; }

    // Holiday debt
    public int MaxStatutoryDebtDays { get; set; }

    // Transfer deadlines
    public int FifthWeekTransferDeadlineMonth { get; set; }
    public int FifthWeekTransferDeadlineDay { get; set; }

    public int SixthWeekTransferDeadlineMonth { get; set; }
    public int SixthWeekTransferDeadlineDay { get; set; }
}