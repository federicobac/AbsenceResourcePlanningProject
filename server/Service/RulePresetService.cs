using System.ComponentModel.DataAnnotations;
using Infra;
using LinqToDB;
using Service.RequestDtos;

namespace Service;

public class RulePresetService(MyDatabaseConnection db)
{
    private static DateOnly Today()
    {
        var timezone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

        var danishTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);

        return DateOnly.FromDateTime(danishTime);
    }

    private static void EnsureScheduled(RulePresetPeriod period)
    {
        if (period.ValidFrom <= Today())
            throw new ValidationException(
                "This preset period is already active or historical " +
                "and cannot be modified or deleted.");
    }

    private void ValidatePreset(string code)
    {
        if (!db.RulePresets.Any(p => p.RulePresetCode == code))
            throw new ValidationException("Invalid rule preset");
    }
    
    public List<RulePresetDto> GetPresets()
    {
        return db.RulePresets
            .OrderBy(p => p.RulePresetCode)
            .ToList()
            .Select(p => new RulePresetDto(p))
            .ToList();
    }
    
    public List<RulePresetPeriodDto> GetPresetPeriods()
    {
        return db.RulePresetPeriods
            .OrderBy(p => p.ValidFrom)
            .ToList()
            .Select(p => new RulePresetPeriodDto(p))
            .ToList();
    }
    
    public RulePresetPeriodDto GetApplicablePreset(DateOnly date)
    {
        var period = db.RulePresetPeriods
            .Where(p =>
                p.ValidFrom <= date &&
                (!p.ValidUntil.HasValue ||
                 p.ValidUntil.Value >= date))
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefault();

        if (period is null)
            throw new ValidationException("No rule preset found for the specified date");

        return new RulePresetPeriodDto(period);
    }
    
    public RulePresetPeriodDto CreatePresetPeriod(
        CreateRulePresetPeriodRequestDto dto)
    {
        if (dto.ValidFrom < Today())
            throw new ValidationException("A preset cannot start in the past");

        ValidatePreset(dto.RulePresetCode);

        var periods = db.RulePresetPeriods
            .OrderBy(p => p.ValidFrom)
            .ToList();

        // Only one future selection is allowed
        if (periods.Any(p => p.ValidFrom > Today()))
            throw new ValidationException(
                "A future preset change is already scheduled. " +
                "Update or delete it before creating another.");

        var previous = periods.LastOrDefault();

        if (previous != null &&
            dto.ValidFrom <= previous.ValidFrom)
            throw new ValidationException("New preset must start after the previous preset");

        var period = new RulePresetPeriod
        {
            RulePresetPeriodId = Guid.NewGuid().ToString(),
            RulePresetCode = dto.RulePresetCode,
            ValidFrom = dto.ValidFrom,
            ValidUntil = null
        };

        using var transaction = db.BeginTransaction();

        if (previous != null)
        {
            previous.ValidUntil = dto.ValidFrom.AddDays(-1);
            db.Update(previous);
        }

        db.Insert(period);

        transaction.Commit();

        return new RulePresetPeriodDto(period);
    }

    public RulePresetPeriodDto UpdatePresetPeriod(
        string id,
        UpdateRulePresetPeriodRequestDto dto)
    {
        var period = db.RulePresetPeriods
            .FirstOrDefault(p => p.RulePresetPeriodId == id);

        if (period is null)
            throw new ValidationException("Preset period not found");

        EnsureScheduled(period);

        if (dto.ValidFrom < Today())
            throw new ValidationException("A preset cannot start in the past");

        ValidatePreset(dto.RulePresetCode);

        var previous = db.RulePresetPeriods
            .Where(p => p.ValidFrom < period.ValidFrom)
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefault();

        if (previous is null)
            throw new ValidationException("The initial preset cannot be modified");

        if (dto.ValidFrom <= previous.ValidFrom)
            throw new ValidationException("New date must be after the previous preset");

        using var transaction = db.BeginTransaction();

        previous.ValidUntil = dto.ValidFrom.AddDays(-1);
        db.Update(previous);

        period.RulePresetCode = dto.RulePresetCode;
        period.ValidFrom = dto.ValidFrom;

        db.Update(period);

        transaction.Commit();

        return new RulePresetPeriodDto(period);
    }
    
    public void DeletePresetPeriod(string id)
    {
        var period = db.RulePresetPeriods
            .FirstOrDefault(p => p.RulePresetPeriodId == id);

        if (period is null)
            throw new ValidationException("Preset period not found");

        EnsureScheduled(period);

        var previous = db.RulePresetPeriods
            .Where(p => p.ValidFrom < period.ValidFrom)
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefault();

        if (previous is null)
            throw new ValidationException("The initial preset cannot be deleted");

        using var transaction = db.BeginTransaction();

        previous.ValidUntil = null;
        db.Update(previous);

        db.Delete(period);

        transaction.Commit();
    }
}