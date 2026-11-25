using LinqToDB;
using LinqToDB.Data;

namespace Infra;

public class MyDatabaseConnection(DataOptions<MyDatabaseConnection> options) : DataConnection(options.Options)
{
    public ITable<User> Users => this.GetTable<User>();
    public ITable<HolidayDate> HolidayDates => this.GetTable<HolidayDate>();
    public ITable<WorkingSchedule>  WorkingSchedules => this.GetTable<WorkingSchedule>();
    
    public ITable<GlobalRuleSet>  GlobalRuleSets => this.GetTable<GlobalRuleSet>();
    public ITable<RulePreset>  RulePresets => this.GetTable<RulePreset>();
    public ITable<RulePresetPeriod>  RulePresetPeriods => this.GetTable<RulePresetPeriod>();
    public ITable<HolidayAccount> HolidayAccounts => this.GetTable<HolidayAccount>();
    public ITable<HolidayAccountEntry> HolidayAccountEntries => this.GetTable<HolidayAccountEntry>();
}