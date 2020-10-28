using System;
using System.Net;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Commons
{
    [Serializable]
    public class UserAlreadyExistException : Exception
    {
        public UserAlreadyExistException()
        {            
        }

        public UserAlreadyExistException(string name) : base(String.Format("User already exist: {0}", name))
        {
        }
    }
}
