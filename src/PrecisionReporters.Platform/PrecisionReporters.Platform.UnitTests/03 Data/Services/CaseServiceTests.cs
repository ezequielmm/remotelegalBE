using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Domain.Services;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Services
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

            _caseRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Case, bool>>>(),It.IsAny<string>())).ReturnsAsync(_cases);
            _caseRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(() => _cases.FirstOrDefault());

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
                    CreationDate = DateTime.UtcNow
                },
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase2",
                    CreationDate = DateTime.UtcNow
                },
                new Case
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCase3",
                    CreationDate = DateTime.UtcNow
                }
            });

            // Act
            var result = await _service.GetCases();

            // Assert
            _caseRepositoryMock.Verify(mock => mock.GetByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string>()), Times.Once());
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
                CreationDate = DateTime.Now
            });

            // Act
            var result = await _service.GetCaseById(id);

            // Assert
            _caseRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a=>a==id), It.IsAny<string>()), Times.Once());
            Assert.NotNull(result);
            Assert.IsType<Case>(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public async Task CreateCase_ShouldReturn_NewlyCretedCase_WithGivenName()
        {
            // Arrange
            var name = "Test";
            var newCase = new Case { CreationDate = DateTime.Now, Name = name };
            _caseRepositoryMock.Setup(x => x.Create(It.IsAny<Case>()))
                .Returns<Case>((a) =>
                {
                    a.Id = Guid.NewGuid();
                    return Task.FromResult(a);
                })
                .Verifiable();

            // Act
            var result = await _service.CreateCase(newCase);

            // Assert
            _caseRepositoryMock.Verify(mock => mock.Create(It.Is<Case>(a=>a== newCase)), Times.Once());
            Assert.NotNull(result);
            Assert.Equal(name, result.Name);
        }
    }
}
