using System;
namespace PrecisionReporters.Platform.Api.Dtos
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
