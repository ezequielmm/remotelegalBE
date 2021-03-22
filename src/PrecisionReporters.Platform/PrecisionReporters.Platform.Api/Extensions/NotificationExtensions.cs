﻿using PrecisionReporters.Platform.Domain.Commons;
using System;

namespace PrecisionReporters.Platform.Api.Extensions
{
    public static class NotificationExtensions        
    {
        public static string GetDepositionSignalRGroupName(this Guid depositionId)
        {
            return $"{ApplicationConstants.DepositionGroupName}{depositionId}";
        }
    }
}