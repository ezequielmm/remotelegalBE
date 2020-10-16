using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


namespace PrecisionReporters.Platform.UnitTests.Services
{
    public class CaseServiceTests : IDisposable
    {
        private readonly CaseService _service;
        private readonly Mock<ICaseRepository> _caseRepositoryMock;

        private List<Case> _cases = new List<Case>();

        public CaseServiceTests()
        {
            // Setup
            _caseRepositoryMock = new Mock<ICaseRepository>();

            _caseRepositoryMock.Setup(x => x.GetCases()).ReturnsAsync(_cases);
            _caseRepositoryMock.Setup(x => x.GetCaseById(It.IsAny<Guid>())).ReturnsAsync(() => _cases.FirstOrDefault());

            _service = new CaseService(_caseRepositoryMock.Object);
        }
        public void Dispose()
        {
            // Tear down
        }

        [Fact]
        public async Task GetCases_ShouldReturn_ListOfAllCases()
        {
            // Arrange
            _cases.AddRange(new List<Case> {
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase1",
                    CreatedDate = DateTime.UtcNow
                },
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase2",
                    CreatedDate = DateTime.UtcNow
                },
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase3",
                    CreatedDate = DateTime.UtcNow
                }
            });

            // Act
            var result = await _service.GetCases();

            // Assert
            _caseRepositoryMock.Verify(mock => mock.GetCases(), Times.Once());
            Assert.NotEmpty(result);
            Assert.Equal(_cases.Count, result.Count);
        }

        [Fact]
        public async Task GetCaseById_ShouldReturn_CasesWithGivenId()
        {
            // Arrange
            var id = Guid.NewGuid();
            _cases.Add(new Case
            {
                Id = id,
                Name = "TestCase1",
                CreatedDate = DateTime.Now
            });

            // Act
            var result = await _service.GetCaseById(id);

            // Assert
            _caseRepositoryMock.Verify(mock => mock.GetCaseById(id), Times.Once());
            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public async Task CreateCase_ShouldReturn_NewlyCretedCase_WithGivenName()
        {
            // Arrange
            var name = "Test";
            var newCase = new Case { CreatedDate = DateTime.Now, Name = name };
            _caseRepositoryMock.Setup(x => x.CreateCase(It.IsAny<Case>()))
                .Returns<Case>((a) =>
                {
                    a.Id = Guid.NewGuid();
                    return Task.FromResult(a);
                })
                .Verifiable();

            // Act
            var result = await _service.CreateCase(newCase);

            // Assert
            _caseRepositoryMock.Verify(mock => mock.CreateCase(It.IsAny<Case>()), Times.Once());
            Assert.NotNull(result);
            Assert.Equal(name, result.Name);
        }
    }
}
