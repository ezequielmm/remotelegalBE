using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Services
{
    public class VerifyUserServiceTests
    {
        private readonly Mock<IVerifyUserRepository> _verifyUserRepositoryMock = new Mock<IVerifyUserRepository>();

        [Fact]
        public async Task GetVerifyUserById_ShouldCall_GetById()
        {
            var id = Guid.NewGuid();
            _verifyUserRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync((VerifyUser)null);
            var service = InitializeService(_verifyUserRepositoryMock);

            // Act
            await service.GetVerifyUserById(id);

            // Assert
            _verifyUserRepositoryMock.Verify(x => x.GetById(It.Is<Guid>((a) => a == id), It.Is<string[]>(a => a.Contains(nameof(VerifyUser.User)))), Times.Once);
        }

        [Fact]
        public async Task GetVerifyUserByUserId_ShouldCall_GetByUserId()
        {
            var id = Guid.NewGuid();
            _verifyUserRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<VerifyUser, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((VerifyUser)null);
            var service = InitializeService(_verifyUserRepositoryMock);

            // Act
            await service.GetVerifyUserByUserId(id);

            // Assert
            _verifyUserRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<VerifyUser, bool>>>(), It.Is<string[]>(a => a.Contains(nameof(VerifyUser.User)))), Times.Once);
        }

        [Fact]
        public async Task CreateVerifyUser_ShouldCall_Create()
        {
            var verifyUser = new VerifyUser();

            _verifyUserRepositoryMock.Setup(x => x.Create(It.IsAny<VerifyUser>()))
                .ReturnsAsync((VerifyUser)null);
            var service = InitializeService(_verifyUserRepositoryMock);

            // Act
            await service.CreateVerifyUser(verifyUser);

            // Assert
            _verifyUserRepositoryMock.Verify(x => x.Create(It.Is<VerifyUser>((a) => a == verifyUser)), Times.Once);
        }

        [Fact]
        public async Task UpdateVerifyUser_ShouldCall_Update()
        {
            var verifyUser = new VerifyUser();

            _verifyUserRepositoryMock.Setup(x => x.Update(It.IsAny<VerifyUser>()))
                .ReturnsAsync((VerifyUser)null);
            var service = InitializeService(_verifyUserRepositoryMock);

            // Act
            await service.UpdateVerifyUser(verifyUser);

            // Assert
            _verifyUserRepositoryMock.Verify(x => x.Update(It.Is<VerifyUser>((a) => a == verifyUser)), Times.Once);
        }

        private VerifyUserService InitializeService(Mock<IVerifyUserRepository> verifyUserRepository)
        {
            var verifyUserRepositoryMock = verifyUserRepository ?? new Mock<IVerifyUserRepository>();

            return new VerifyUserService(verifyUserRepositoryMock.Object);
        }
    }
}
