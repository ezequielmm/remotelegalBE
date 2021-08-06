using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class SystemSettingsTests
    {
        private readonly Mock<ISystemSettingsRepository> _repository;
        private readonly SystemSettingsService _service;
        public SystemSettingsTests()
        {
            _repository = new Mock<ISystemSettingsRepository>();
            _service = new SystemSettingsService(_repository.Object);
        }


        [Fact]
        public async Task GetAll_ShouldOK()
        {
            // Arrange
            var mockRepositoryResult = new List<SystemSettings> { new SystemSettings { Name = SystemSettingsName.EnableBreakrooms, Value = "Test" } };
            _repository.Setup(s => s.GetByFilter(It.IsAny<Expression<Func<SystemSettings, bool>>>(),
                It.IsAny<string[]>())).ReturnsAsync(mockRepositoryResult);

            // Act
            var result = await _service.GetAll();

            // Assert
            _repository.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<SystemSettings, bool>>>(),
                It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }
    }
}
