using System;
namespace PrecisionReporters.Platform.Domain.Commons
{
    [Serializable]
    public class InvalidArgumentException : BaseException
    {
        public InvalidArgumentException(string message) : base(400, message)
        {
        }
    }
}
