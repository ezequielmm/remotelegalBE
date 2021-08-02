using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class CameraDto
    {
        public string Name { get; set; }
        public CameraStatus? Status { get; set; }
    }
}
