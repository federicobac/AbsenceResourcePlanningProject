using Facet;
using Infra;

namespace Service.RequestDtos;

[Facet(sourceType: typeof(WorkingSchedule),
    exclude:
    [
        nameof(WorkingSchedule.WorkingScheduleId),
        nameof(WorkingSchedule.UserId)
    ])] 
public partial class CreateWorkingScheduleRequestDto;