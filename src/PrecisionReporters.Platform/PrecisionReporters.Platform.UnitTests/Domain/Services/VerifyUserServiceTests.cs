using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
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
        public async Task GetVerifyUserById_ShouldVerifyUser_GetById()
        {
            var id = Guid.NewGuid();
            var verifyUser = new VerifyUser { Id = id };
            _verifyUserRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(verifyUser);
            var service = InitializeService(_verifyUserRepositoryMock);

            // Act
            var result = await service.GetVerifyUserById(id);

            // Assert
            _verifyUserRepositoryMock.Verify(x => x.GetById(It.Is<Guid>((a) => a == id), It.Is<string[]>(a => a.Contains(nameof(VerifyUser.User)))), Times.Once);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetVerifyUserByUserId_ShouldVerifyUser_WhenVerificationTypeIsNotIncluded()
        {
            var id = Guid.NewGuid();
            var verifyUser = new VerifyUser { Id = id };
            _verifyUserRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<VerifyUser, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(verifyUser);
            var service = InitializeService(_verifyUserRepositoryMock);

            // Act
            var result = await service.GetVerifyUserByUserId(id);

            // Assert
            _verifyUserRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<VerifyUser, bool>>>(), It.Is<string[]>(a => a.Contains(nameof(VerifyUser.User))), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetVerifyUserByUserId_ShouldVerifyUser_WhenVerificationTypeIsIncluded()
        {
            var id = Guid.NewGuid();
            var verifyUser = new VerifyUser { Id = id };
            _verifyUserRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<VerifyUser, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(verifyUser);
            var service = InitializeService(_verifyUserRepositoryMock);

            // Act
            var result = await service.GetVerifyUserByUserId(id, VerificationType.VerifyUser);

            // Assert
            _verifyUserRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<VerifyUser, bool>>>(), It.Is<string[]>(a => a.Contains(nameof(VerifyUser.User))), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(result);
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

        [Fact]
        public async Task GetVerifyUserByEmail_ShouldVerifyUser_WhenVerificationTypeIsNotIncluded()
        {
            var email = "User1@TestMail.com";
            var verifyUser = new VerifyUser { Id = Guid.NewGuid() };
            _verifyUserRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<VerifyUser, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(verifyUser);
            var service = InitializeService(_verifyUserRepositoryMock);

            // Act
            var result = await service.GetVerifyUserByEmail(email);

            // Assert
            _verifyUserRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<VerifyUser, bool>>>(), It.Is<string[]>(a => a.Contains(nameof(VerifyUser.User))), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetVerifyUserByEmail_ShouldVerifyUser_WhenVerificationTypeIsIncluded()
        {
            var email = "User1@TestMail.com";
            var verifyUser = new VerifyUser { Id = Guid.NewGuid() };
            _verifyUserRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<VerifyUser, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(verifyUser);
            var service = InitializeService(_verifyUserRepositoryMock);

            // Act
            var result = await service.GetVerifyUserByEmail(email, VerificationType.VerifyUser);

            // Assert
            _verifyUserRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<VerifyUser, bool>>>(), It.Is<string[]>(a => a.Contains(nameof(VerifyUser.User))), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(result);
        }

        private VerifyUserService InitializeService(Mock<IVerifyUserRepository> verifyUserRepository)
        {
            var verifyUserRepositoryMock = verifyUserRepository ?? new Mock<IVerifyUserRepository>();

            return new VerifyUserService(verifyUserRepositoryMock.Object);
        }
    }
}
