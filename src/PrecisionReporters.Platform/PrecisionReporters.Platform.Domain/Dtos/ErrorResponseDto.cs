using System;
namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class ErrorResponseDto
    {
        public string Message { get; set; }
        public Exception Error { get; set; }

        public ErrorResponseDto()
        {
        }
    }
}
