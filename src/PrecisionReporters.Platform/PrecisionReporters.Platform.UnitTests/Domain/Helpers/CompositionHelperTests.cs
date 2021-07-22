using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Helpers
{
    public class CompositionHelperTests : IDisposable
    {
        private readonly CompositionHelper _compositionHelper;

        public CompositionHelperTests()
        {
            _compositionHelper = new CompositionHelper();
        }

        public void Dispose()
        {
        }

        [Fact]
        public async Task GetDepositionRecordingIntervals()
        {
            var events = new List<DepositionEvent>();
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow, EventType = EventType.StartDeposition });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(1), EventType = EventType.OnTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(25), EventType = EventType.OffTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(56), EventType = EventType.OnTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow, EventType = EventType.StartDeposition });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(125), EventType = EventType.OffTheRecord });
            var result = _compositionHelper.GetDepositionRecordingIntervals(events, DateTime.UtcNow);
            

            Assert.Equal(2, result.Count);
        }
    }
}
