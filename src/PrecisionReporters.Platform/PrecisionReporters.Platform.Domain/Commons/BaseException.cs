using System;

namespace PrecisionReporters.Platform.Domain.Commons
{
    [Serializable]
    public class BaseException : Exception
    {
        public int StatusCode { get; set; }

        public BaseException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
