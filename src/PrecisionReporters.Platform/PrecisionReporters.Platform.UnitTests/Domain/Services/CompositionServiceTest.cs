using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using Xunit;
using Microsoft.Extensions.Logging;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class CompositionServiceTest : IDisposable
    {
        private readonly CompositionService _service;
        private readonly Mock<ICompositionRepository> _compositionRepositoryMock;
        private readonly Mock<ITwilioService> _twilioServiceMock;
        private readonly Mock<IRoomService> _roomServiceMock;
        private readonly Mock<IDepositionService> _depositionServiceMock;
        private readonly Mock<ILogger<CompositionService>> _loggerMock;

        public CompositionServiceTest()
        {
            _compositionRepositoryMock = new Mock<ICompositionRepository>();
            _twilioServiceMock = new Mock<ITwilioService>();
            _depositionServiceMock = new Mock<IDepositionService>();
            _roomServiceMock = new Mock<IRoomService>();
            _loggerMock = new Mock<ILogger<CompositionService>>();

            _service = new CompositionService(_compositionRepositoryMock.Object, _twilioServiceMock.Object,
                _roomServiceMock.Object, _depositionServiceMock.Object, _loggerMock.Object);
        }

        public void Dispose()
        {
        }

        [Fact]
        public async Task GetDepositionRecordingIntervals()
        {
            var events = new List<DepositionEvent>();
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow, EventType = EventType.StartDeposition });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(1), EventType = EventType.OnTheRecord});
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(25), EventType = EventType.OffTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(56), EventType = EventType.OnTheRecord });
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow, EventType = EventType.StartDeposition }); 
            events.Add(new DepositionEvent { CreationDate = DateTime.UtcNow.AddSeconds(125), EventType = EventType.OffTheRecord });
            var result = _service.GetDepositionRecordingIntervals(events, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());

            Assert.Equal(2, result.Count);
        }
    }
}
