using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;

namespace PrecisionReporters.Platform.Domain.Helpers
{
    public class CompositionHelper : ICompositionHelper
    {
        public List<CompositionInterval> GetDepositionRecordingIntervals(List<DepositionEvent> events, long startTime)
        {
            var result = events
                .OrderBy(x => x.CreationDate)
                .Where(x => x.EventType == EventType.OnTheRecord || x.EventType == EventType.OffTheRecord)
                .Aggregate(new List<CompositionInterval>(),
                (list, x) =>
                {
                    if (x.EventType == EventType.OnTheRecord)
                    {
                        var compositionInterval = new CompositionInterval
                        {
                            Start = CalculateSeconds(startTime, GetDateTimestamp(x.CreationDate))
                        };
                        list.Add(compositionInterval);
                    }
                    if (x.EventType == EventType.OffTheRecord)
                        list.Last().Stop = CalculateSeconds(startTime, GetDateTimestamp(x.CreationDate));

                    return list;
                });

            return result;
        }

        public long GetDateTimestamp(DateTime date)
        {
            return new DateTimeOffset(date, TimeSpan.Zero).ToUnixTimeMilliseconds();
        }

        private int CalculateSeconds(long startTime, long splitTime)
        {
            return (int)(splitTime - startTime);
        }        
    }
}
