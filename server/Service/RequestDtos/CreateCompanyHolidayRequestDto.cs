using Facet;
using Infra;

namespace Service.RequestDtos;

[Facet(sourceType: typeof(HolidayDate),
    exclude:
    [
        nameof(HolidayDate.HolidayDateId),
        nameof(HolidayDate.Type)
    ])]
public partial class CreateCompanyHolidayRequestDto;