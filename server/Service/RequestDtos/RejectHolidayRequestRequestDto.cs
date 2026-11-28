using Facet;
using Infra;

namespace Service.RequestDtos;

[Facet(sourceType: typeof(HolidayRequest),
    exclude:
    [
        nameof(HolidayRequest.HolidayRequestId),
        nameof(HolidayRequest.UserId),
        nameof(HolidayRequest.AccountType),
        nameof(HolidayRequest.StartDate),
        nameof(HolidayRequest.EndDate),
        nameof(HolidayRequest.RequestedHours),
        nameof(HolidayRequest.Status),
        nameof(HolidayRequest.Comment),
        nameof(HolidayRequest.CreatedAtUtc),
        nameof(HolidayRequest.ReviewedByUserId),
        nameof(HolidayRequest.ReviewedAtUtc)
    ])]
public partial class RejectHolidayRequestRequestDto;