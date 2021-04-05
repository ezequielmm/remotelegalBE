using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class BackgroundTaskDto
    {
        public BackgroundTaskType TaskType { get; set; }
        public object Content { get; set; }
    }
}
