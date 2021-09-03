using FluentResults;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class PermissionServiceTests : IDisposable
    {
        private readonly PermissionService _permissionService;
        private readonly Mock<IRoleRepository> _roleRepositoryMock;
        private readonly Mock<IUserResourceRoleRepository> _userResourceRoleRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;

        public PermissionServiceTests()
        {
            _roleRepositoryMock = new Mock<IRoleRepository>();
            _userResourceRoleRepositoryMock = new Mock<IUserResourceRoleRepository>();
            _userServiceMock = new Mock<IUserService>();

            _permissionService = new PermissionService(_roleRepositoryMock.Object, _userResourceRoleRepositoryMock.Object,
                _userServiceMock.Object);
        }

        public void Dispose()
        {
            // Tear down
        }

        [Fact]
        public async Task CheckUserHasPermissionForAction_ShouldReturnFalse_IfCantRetrieveUser()
        {
            // Arrange
            var email = "test@test.com";
            var resourceType = ResourceType.Case;
            var resourceId = Guid.NewGuid();
            var action = ResourceAction.View;
            _userServiceMock.Setup(s => s.GetUserByEmail(It.Is<string>(x => x == email))).ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _permissionService.CheckUserHasPermissionForAction(email, resourceType, resourceId, action);

            // Assert
            Assert.False(result);
            _userResourceRoleRepositoryMock.Verify(r => r.CheckUserHasPermissionForAction(It.IsAny<Guid>(), It.IsAny<ResourceType>(), It.IsAny<Guid>(), It.IsAny<ResourceAction>()), Times.Never);
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, false, false)]
        [InlineData(true, true, true)]
        [InlineData(false, true, true)]
        public async Task CheckUserHasPermissionForAction(bool hasPermissions, bool userIsAdmin, bool expectedResult)
        {
            // Arrange
            var email = "test@test.com";
            var resourceType = ResourceType.Case;
            var resourceId = Guid.NewGuid();
            var action = ResourceAction.View;
            var userId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.GetUserByEmail(It.Is<string>(x => x == email))).ReturnsAsync(Result.Ok(new User { Id = userId, EmailAddress = email, IsAdmin = userIsAdmin }));
            _userResourceRoleRepositoryMock
                .Setup(r => r.CheckUserHasPermissionForAction(It.Is<Guid>(x => x == userId), It.Is<ResourceType>(x => x == resourceType), It.Is<Guid>(x => x == resourceId), It.Is<ResourceAction>(x => x == action)))
                .ReturnsAsync(hasPermissions);

            // Act
            var result = await _permissionService.CheckUserHasPermissionForAction(email, resourceType, resourceId, action);

            // Assert
            Assert.Equal(expectedResult, result);
            _userResourceRoleRepositoryMock.Verify(r => r.CheckUserHasPermissionForAction(It.IsAny<Guid>(), It.IsAny<ResourceType>(), It.IsAny<Guid>(), It.IsAny<ResourceAction>()), Times.Exactly(userIsAdmin ? 0 : 1));
        }

        [Fact]
        public async Task AddUserRole_ShouldFail_IfCantFindRoleByName()
        {
            // Arrange
            var resourceType = ResourceType.Case;
            var resourceId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var roleName = RoleName.CaseAdmin;

            // Act
            var result = await _permissionService.AddUserRole(userId, resourceId, resourceType, roleName);

            // Assert
            Assert.True(result.IsFailed);
            _userResourceRoleRepositoryMock.Verify(r => r.Create(It.IsAny<UserResourceRole>()), Times.Never);
        }

        [Fact]
        public async Task AddUserRole_ShouldSucceed()
        {
            // Arrange
            var resourceType = ResourceType.Case;
            var resourceId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var roleName = RoleName.CaseAdmin;
            var role = new Role { Id = Guid.NewGuid(), Name = roleName };
            _roleRepositoryMock.Setup(r => r.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<Role, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(role);

            // Act
            var result = await _permissionService.AddUserRole(userId, resourceId, resourceType, roleName);

            // Assert
            Assert.True(result.IsSuccess);
            _userResourceRoleRepositoryMock.Verify(r => r.Create(It.IsAny<UserResourceRole>()), Times.Once);
        }

        [Fact]
        public async Task GetParticipantPermissions_ShouldFailForNullParticipant()
        {
            // Arrange
            Participant participant = null;
            // Act
            var result = await _permissionService.GetDepositionUserPermissions(participant, Guid.NewGuid());

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GetParticipantPermissions_ShouldReturnACompleteListForAnAdmin()
        {
            // Arrange
            var participant = new Participant();
            // Act
            var result = await _permissionService.GetDepositionUserPermissions(participant, Guid.NewGuid(), true);
            var expectedResult = Enum.GetValues(typeof(ResourceAction)).Cast<ResourceAction>().ToList();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(result.Value, expectedResult);
        }

        [Fact]
        public async Task RemoveParticipantPermissions_ShouldReturnOk()
        {
            //Arrange
            var depositionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var participant = new Participant
            {
                UserId = userId,
                User = new User()
                {
                    Id = userId
                }
            };
            var userResourceRole = new UserResourceRole() { UserId = userId, ResourceId = depositionId };
            _userResourceRoleRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<UserResourceRole, bool>>>(),
                It.IsAny<string[]>())).ReturnsAsync(userResourceRole);
            _userResourceRoleRepositoryMock.Setup(x => x.Remove(It.IsAny<UserResourceRole>()));

            //Act
            await _permissionService.RemoveParticipantPermissions(depositionId, participant);

            //Assert
            _userResourceRoleRepositoryMock.Verify(x => x.Remove(It.IsAny<UserResourceRole>()), Times.Once);
        }
    }
}