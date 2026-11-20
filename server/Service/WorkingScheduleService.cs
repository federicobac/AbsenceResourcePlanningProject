using System.ComponentModel.DataAnnotations;
using Infra;
using LinqToDB;
using Service.RequestDtos;

namespace Service;

public class WorkingScheduleService(MyDatabaseConnection db)
{
    private static decimal CalculateDayHours(
        TimeOnly? from,
        TimeOnly? to)
    {
        if (from is null && to is null)
            return 0;

        if (from is null || to is null)
            throw new ValidationException("Both start and end times are required");

        if (to <= from)
            throw new ValidationException("End time must be after start time");

        return (decimal)(to.Value - from.Value).TotalMinutes / 60m;
    }

    private static decimal CalculateWeeklyHours(CreateWorkingScheduleRequestDto dto)
    {
        return 
            CalculateDayHours(dto.MondayFrom, dto.MondayTo) +
            CalculateDayHours(dto.TuesdayFrom, dto.TuesdayTo)+
            CalculateDayHours(dto.WednesdayFrom, dto.WednesdayTo) +
            CalculateDayHours(dto.ThursdayFrom, dto.ThursdayTo) +
            CalculateDayHours(dto.FridayFrom, dto.FridayTo);
    }

    private static void ValidateSchedule(CreateWorkingScheduleRequestDto dto)
    {
        if (dto.ValidFrom == default)
            throw new ValidationException("ValidFrom is required");
        
        if (dto.ValidUntil.HasValue && dto.ValidUntil.Value < dto.ValidFrom)
            throw new ValidationException("ValidUntil cannot be before ValidFrom");
        
        if (dto.EmploymentType is not ("FullTime" or "PartTime"))
            throw new ValidationException("EmploymentType must be FullTime or PartTime");
        
        decimal weeklyHours = CalculateWeeklyHours(dto);
        
        if (weeklyHours <= 0)
            throw new ValidationException("WeeklyHours must be greater than zero");
    }

    private static WorkingSchedule CreateEntity(string userId, CreateWorkingScheduleRequestDto dto)
    {
        return new WorkingSchedule
        {
            WorkingScheduleId = Guid.NewGuid().ToString(),
            UserId = userId,

            ValidFrom = dto.ValidFrom,
            ValidUntil = dto.ValidUntil,

            EmploymentType = dto.EmploymentType,

            MondayFrom = dto.MondayFrom,
            MondayTo = dto.MondayTo,

            TuesdayFrom = dto.TuesdayFrom,
            TuesdayTo = dto.TuesdayTo,

            WednesdayFrom = dto.WednesdayFrom,
            WednesdayTo = dto.WednesdayTo,

            ThursdayFrom = dto.ThursdayFrom,
            ThursdayTo = dto.ThursdayTo,

            FridayFrom = dto.FridayFrom,
            FridayTo = dto.FridayTo
        };
    }

    public WorkingScheduleDto CreateSchedule(string userId, CreateWorkingScheduleRequestDto dto)
    {
        ValidateSchedule(dto);
        
        if (!db.Users.Any(u => u.UserId== userId))
            throw new ValidationException("User not found");

        var previous = db.WorkingSchedules
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ValidFrom)
            .FirstOrDefault();

        if (previous != null)
        {
           if (dto.ValidFrom <= previous.ValidFrom)
               throw new ValidationException("New schedule must start after previous schedule");

           if (previous.ValidUntil.HasValue &&
               dto.ValidFrom <= previous.ValidUntil.Value)
               throw new ValidationException("New schedule overlaps the previous schedule");
        }

        var schedule = CreateEntity(userId, dto);

        if (previous != null)
        {
            using var transaction = db.BeginTransaction();
            
            if (!previous.ValidUntil.HasValue)
            {
                previous.ValidUntil = dto.ValidFrom.AddDays(-1);
                db.Update(previous);
            }

            db.Insert(schedule);
            
            transaction.Commit();
        }
        else
        {
            db.Insert(schedule);
        }

        return new WorkingScheduleDto(schedule);
    }

    public List<WorkingScheduleDto> GetSchedules(string userId)
    {
        if (!db.Users.Any(u =>u.UserId == userId))
            throw new ValidationException("USer not found");
        return db.WorkingSchedules
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ValidFrom)
            .ToList()
            .Select(s => new WorkingScheduleDto(s))
            .ToList();
    }
}