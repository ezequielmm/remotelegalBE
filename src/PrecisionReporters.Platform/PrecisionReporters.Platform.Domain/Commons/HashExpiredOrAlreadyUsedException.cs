﻿using System;

namespace PrecisionReporters.Platform.Domain.Commons
{
    [Serializable]
    public class HashExpiredOrAlreadyUsedException : BaseException
    {
        public HashExpiredOrAlreadyUsedException(string message) : base(409, message)
        {

        }
    }
}
