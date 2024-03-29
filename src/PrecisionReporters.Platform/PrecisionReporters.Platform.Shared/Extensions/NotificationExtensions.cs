﻿using PrecisionReporters.Platform.Shared.Commons;
using System;

namespace PrecisionReporters.Platform.Shared.Extensions
{
    public static class NotificationExtensions
    {
        public static string GetDepositionSignalRGroupName(this Guid depositionId)
        {
            return $"{ApplicationConstants.DepositionGroupName}{depositionId}";
        }

        public static string GetDepositionSignalRAdminsGroupName(this Guid depositionId)
        {
            return $"{ApplicationConstants.DepositionAdminsGroupName}{depositionId}";
        }
    }
}
