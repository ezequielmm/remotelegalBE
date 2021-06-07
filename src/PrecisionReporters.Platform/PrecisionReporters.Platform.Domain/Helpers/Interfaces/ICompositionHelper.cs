using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;

namespace PrecisionReporters.Platform.Domain.Helpers.Interfaces
{
    public interface ICompositionHelper
    {
        List<CompositionInterval> GetDepositionRecordingIntervals(List<DepositionEvent> events, long startTime);

        long GetDateTimestamp(DateTime date);
    }
}
