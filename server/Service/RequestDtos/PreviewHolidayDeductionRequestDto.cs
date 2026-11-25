namespace Service.RequestDtos;

public record PreviewHolidayDeductionRequestDto(
    DateOnly StartDate,
    DateOnly EndDate
);