using Facet;
using Infra;

namespace Service.RequestDtos;


[Facet(sourceType:typeof(HolidayRequest),
    exclude:
    [
        nameof(HolidayRequest.HolidayRequestId),
        nameof(HolidayRequest.UserId),
        nameof(HolidayRequest.RequestedHours),
        nameof(HolidayRequest.Status),
        nameof(HolidayRequest.CreatedAtUtc),
        nameof(HolidayRequest.ReviewedByUserId),
        nameof(HolidayRequest.ReviewedAtUtc),
        nameof(HolidayRequest.ReviewComment)
    ])]
public partial class CreateHolidayRequestRequestDto;