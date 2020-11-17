using System;

namespace PrecisionReporters.Platform.Domain.Commons
{
    [Serializable]
    public class UserAlreadyExistException : BaseException
    {
        public UserAlreadyExistException(string name) : base(409, String.Format("User already exist: {0}", name))
        {
        }
    }
}
