using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class ReminderServiceTests
    {
        private readonly Mock<IDepositionRepository> _depositionRepositoryMock;
        private readonly Mock<IDepositionEmailService> _depositionEmailServiceMock;
        private readonly Mock<ITransactionHandler> _transactionHandlerMock;
        private readonly Mock<ILogger<ReminderService>> _loggerMock;
        private readonly Mock<IOptions<ReminderConfiguration>> _reminderConfigurationMock;
        private readonly ReminderConfiguration _reminderConfiguration;
        private readonly ReminderService _service;

        public ReminderServiceTests()
        {
            _depositionRepositoryMock = new Mock<IDepositionRepository>();
            _depositionEmailServiceMock = new Mock<IDepositionEmailService>();
            _transactionHandlerMock = new Mock<ITransactionHandler>();
            _loggerMock = new Mock<ILogger<ReminderService>>();
            _reminderConfiguration = new ReminderConfiguration { DailyExecution = "09:00", MinutesBefore = new int[] { 15, 60 }, ReminderRecurrency = 5 };
            _reminderConfigurationMock = new Mock<IOptions<ReminderConfiguration>>();
            _reminderConfigurationMock.Setup(x => x.Value).Returns(_reminderConfiguration);
            _service = new ReminderService(
                _depositionRepositoryMock.Object,
                _depositionEmailServiceMock.Object,
                _transactionHandlerMock.Object,
                _loggerMock.Object,
                _reminderConfigurationMock.Object);
        }

        [Fact]
        public async Task SendReminder_TransactionFail()
        {
            // Arrange
            var errorMessage = "Unable to send reminders";
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task<Result<bool>>>>()))
                .Returns(async (Func<Task<Result<bool>>> action) =>
                {
                    await action();
                    return Result.Fail(errorMessage);
                });

            // Act
            var result = await _service.SendReminder();

            // Assert            
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task SendReminder_ShouldOk()
        {
            // Arrange
            var participants = new List<Participant>
            {
                new Participant{Email="Test@test.com" },
                new Participant{Email="Test@test.com" },
            };
            var depositions = new List<Deposition>
            {
                new Deposition{ Id = Guid.NewGuid(), Participants = participants},
                new Deposition{ Id = Guid.NewGuid(), Participants = participants}
            };
            var numberOfReminders = depositions.Count * participants.Count * _reminderConfiguration.MinutesBefore.Length;
            _depositionRepositoryMock.Setup(d => d.GetByFilter(It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(depositions);
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task<Result<bool>>>>()))
                .Returns(async (Func<Task<Result<bool>>> action) =>
                {
                    await action();
                    return Result.Ok(true);
                });

            // Act
            var result = await _service.SendReminder();

            // Assert
            _depositionRepositoryMock.Verify(d => d.GetByFilter(It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>()), Times.Exactly(_reminderConfiguration.MinutesBefore.Length));
            _depositionEmailServiceMock.Verify(e => e.SendDepositionReminder(It.IsAny<Deposition>(), It.IsAny<Participant>()), Times.Exactly(numberOfReminders));
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }


        [Fact]
        public async Task SendDailyReminder_TransactionFail()
        {
            // Arrange
            var errorMessage = "Unable to send reminders";
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task<Result<bool>>>>()))
                .Returns(async (Func<Task<Result<bool>>> action) =>
                {
                    await action();
                    return Result.Fail(errorMessage);
                });

            // Act
            var result = await _service.SendDailyReminder();

            // Assert            
            Assert.NotNull(result);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task SendDailyReminder_ShouldOk()
        {
            // Arrange
            var participants = new List<Participant>
            {
                new Participant{Email="Test@test.com" },
                new Participant{Email="Test@test.com" },
            };
            var depositions = new List<Deposition>
            {
                new Deposition{ Id = Guid.NewGuid(), Participants = participants},
                new Deposition{ Id = Guid.NewGuid(), Participants = participants}
            };
            var numberOfReminders = depositions.Count * participants.Count;
            _depositionRepositoryMock.Setup(d => d.GetByFilter(It.IsAny<Expression<Func<Deposition, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(depositions);
            _transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task<Result<bool>>>>()))
                .Returns(async (Func<Task<Result<bool>>> action) =>
                {
                    await action();
                    return Result.Ok(true);
                });
            // Act
            var result = await _service.SendDailyReminder();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }
    }
}
