using System;
namespace PrecisionReporters.Platform.Domain.Commons
{
    [Serializable]
    public class NotFoundException : BaseException
    {
        public NotFoundException(string message) : base(404, message)
        {
        }
    }
}
