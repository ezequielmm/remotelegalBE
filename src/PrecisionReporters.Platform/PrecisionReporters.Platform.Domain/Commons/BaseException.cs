using System;
using System.Net;

namespace PrecisionReporters.Platform.Domain.Commons
{
    public class BaseException : Exception
    {
        public int StatusCode { get; set; } = (int)HttpStatusCode.InternalServerError;

        public BaseException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
