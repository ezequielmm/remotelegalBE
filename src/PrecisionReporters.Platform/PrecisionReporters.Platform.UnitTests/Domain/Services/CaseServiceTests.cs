using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Services
{
    public class CaseServiceTests : IDisposable
    {
        private readonly CaseService _service;
        private readonly Mock<ICaseRepository> _caseRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;

        private List<Case> _cases = new List<Case>();

        public CaseServiceTests()
        {
            // Setup
            _caseRepositoryMock = new Mock<ICaseRepository>();

            _caseRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(_cases);
            _caseRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(() => _cases.FirstOrDefault());

            _userServiceMock = new Mock<IUserService>();
            _service = new CaseService(_caseRepositoryMock.Object, _userServiceMock.Object);
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
            _caseRepositoryMock.Verify(mock => mock.GetByFilter(It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()), Times.Once());
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
            _caseRepositoryMock.Verify(mock => mock.GetById(It.Is<Guid>(a => a == id), It.IsAny<string>()), Times.Once());
            Assert.True(result.IsSuccess);

            var foundCase = result.Value;
            Assert.NotNull(foundCase);
            Assert.Equal(id, foundCase.Id);
        }

        [Fact]
        public async Task CreateCase_ShouldReturn_NewlyCreatedCase_WithGivenName()
        {
            // Arrange
            var name = "Test";
            var userEmail = "TestUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            var newCase = new Case { CreationDate = DateTime.Now, Name = name };
            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(user);
            _caseRepositoryMock.Setup(x => x.Create(It.IsAny<Case>()))
                .Returns<Case>((a) =>
                {
                    a.Id = Guid.NewGuid();
                    return Task.FromResult(a);
                })
                .Verifiable();

            // Act
            var result = await _service.CreateCase(userEmail, newCase);

            // Assert
            _userServiceMock.Verify(mock => mock.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);
            _caseRepositoryMock.Verify(mock => mock.Create(It.Is<Case>(a => a == newCase)), Times.Once());
            Assert.True(result.IsSuccess);

            var createdCase = result.Value;
            Assert.NotNull(createdCase);
            Assert.Equal(name, createdCase.Name);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(CaseSortField.AddedBy, SortDirection.Ascend)]
        [InlineData(CaseSortField.Name, SortDirection.Descend)]
        [InlineData(CaseSortField.CaseNumber, SortDirection.Ascend)]
        [InlineData(CaseSortField.CreatedDate, null)]
        public async Task GetCasesForUser_ShouldReturn_ListOfCases_WhereLogedUserIsMemberOf(CaseSortField? orderBy, SortDirection? sortDirection)
        {
            var userEmail = "testUser@mail.com";
            var user = UserFactory.GetUserByGivenEmail(userEmail);
            var userCases = new List<Case>{
                new Case
            {
                Id = Guid.NewGuid(),
                Name = "TestCase1",
                CreationDate = DateTime.UtcNow
            }};

            _userServiceMock.Setup(x => x.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(user);
            _caseRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<Case, object>>>(), It.IsAny<SortDirection>(), It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(userCases);

            await _service.GetCasesForUser(userEmail, orderBy, sortDirection);

            _userServiceMock.Verify(x => x.GetUserByEmail(It.Is<string>(a => a == userEmail)), Times.Once);

            // TODO: Find a way to evaluate that the param orderBy was called with the given field or the default one
            _caseRepositoryMock.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<Case, object>>>(), It.Is<SortDirection>(a => a == sortDirection || (a == SortDirection.Ascend && sortDirection == null)), It.IsAny<Expression<Func<Case, bool>>>(), It.IsAny<string[]>()), Times.Once);
        }
    }
}
