using Microsoft.Extensions.Configuration;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class UserResourceRoleRepositoryTest
    {
        private readonly DataAccessContextForTest _dataAccessUserResourceRole;

        private UserResourceRoleRepository rolePermissionResult;

        private List<UserResourceRole> _userRoleList;

        private List<UserResourceRole> userRoleList;

        private List<RolePermission> rolePermissionList;

        private readonly Guid resourceId = Guid.Parse("3c0b1013-cefa-4369-b4ef-c47d5675b490");
        private readonly Guid userId = Guid.Parse("bcd954e8-038c-48d6-b089-90e019d6c2dd");
        private readonly Guid caseAtendeeRoleId = Guid.Parse("76ddfd75-c7a2-417f-8eb2-ba9883c2ac6c");

        public UserResourceRoleRepositoryTest()
        {
            _userRoleList = new List<UserResourceRole>();

            userRoleList = new List<UserResourceRole>();

            rolePermissionList = new List<RolePermission>();

            _dataAccessUserResourceRole = new DataAccessContextForTest(Guid.NewGuid());
            _dataAccessUserResourceRole.Database.EnsureDeleted();
            _dataAccessUserResourceRole.Database.EnsureCreated();

        }

        private async Task SeedDb()
        {
            userRoleList = new List<UserResourceRole> {
                new UserResourceRole
                {
                    CreationDate = new DateTime(),
                    ResourceId = resourceId,
                    ResourceType = Shared.Enums.ResourceType.Case,
                    Role = new Role { CreationDate = new DateTime(), Id = caseAtendeeRoleId, Name = RoleName.DepositionAttendee },
                    RoleId = caseAtendeeRoleId,
                    User = new User
                        {
                            Id = userId,
                            CreationDate = new DateTime(),
                            FirstName = "FirstNameUser1",
                            LastName = "LastNameUser1",
                            EmailAddress = "test@test.com",
                            Password = "123456",
                            PhoneNumber = "1234567890",
                            IsAdmin = false,
                        },
                    UserId = userId,
                    Id = caseAtendeeRoleId
                    },
                };

            rolePermissionList = new List<RolePermission> { new RolePermission { RoleId = caseAtendeeRoleId, Action = ResourceAction.View, CreationDate = DateTime.UtcNow, Role = userRoleList[0].Role } };

            _dataAccessUserResourceRole.RolePermissions.AddRange(rolePermissionList);

            _userRoleList.AddRange(userRoleList);

            rolePermissionResult = new UserResourceRoleRepository(_dataAccessUserResourceRole);

            await rolePermissionResult.CreateRange(_userRoleList);
        }

        [Fact]
        public async Task CheckUserHasPermissionForAction_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();
            bool expectedResult = true;

            // act
            var result = await rolePermissionResult.CheckUserHasPermissionForAction(userId, ResourceType.Case, resourceId, ResourceAction.View);

            // assert
            Assert.Equal(expectedResult.ToString(), result.ToString());

        }

        [Fact]
        public async Task GetUserActionsForResource_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            // act
            var result = await rolePermissionResult.GetUserActionsForResource(userId, ResourceType.Case, resourceId);

            // assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateUserResourceRole_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            UserResourceRole newUserResourceRole = new UserResourceRole
            {
                CreationDate = new DateTime(),
                ResourceId = Guid.Parse("c3d398d7-7b06-4dbf-88d2-1feacd9514ef"),
                ResourceType = Shared.Enums.ResourceType.Case,
                Role = new Role { CreationDate = new DateTime(), Id = Guid.Parse("3dfedaf0-3337-4bec-9f75-f64dbcc2a412"), Name = RoleName.DepositionAttendee },
                RoleId = Guid.Parse("3dfedaf0-3337-4bec-9f75-f64dbcc2a412"),
                User = new User
                {
                    Id = Guid.Parse("80f4e220-e694-4eaf-8299-a87bf4afa468"),
                    CreationDate = new DateTime(),
                    FirstName = "FirstNameUser1",
                    LastName = "LastNameUser1",
                    EmailAddress = "test@test.com",
                    Password = "123456",
                    PhoneNumber = "1234567890",
                    IsAdmin = false,
                },
                UserId = Guid.Parse("80f4e220-e694-4eaf-8299-a87bf4afa468"),
                Id = Guid.Parse("3dfedaf0-3337-4bec-9f75-f64dbcc2a412")
            };

            // act
            var result = await rolePermissionResult.Create(newUserResourceRole);

            // assert
            Assert.NotNull(result);
            Assert.Equal(result, newUserResourceRole);
        }

        [Fact]
        public async Task RemoveUserResourceRole_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            UserResourceRole userResourceRoleToRemove = new UserResourceRole
            {
                CreationDate = new DateTime(),
                ResourceId = resourceId,
                ResourceType = Shared.Enums.ResourceType.Case,
                Role = new Role { CreationDate = new DateTime(), Id = caseAtendeeRoleId, Name = RoleName.DepositionAttendee },
                RoleId = caseAtendeeRoleId,
                User = new User
                {
                    Id = userId,
                    CreationDate = new DateTime(),
                    FirstName = "FirstNameUser1",
                    LastName = "LastNameUser1",
                    EmailAddress = "test@test.com",
                    Password = "123456",
                    PhoneNumber = "1234567890",
                    IsAdmin = false,
                }
            };


            // act
            var result = rolePermissionResult.Remove(userResourceRoleToRemove);

            // assert
            Assert.Equal(Task.CompletedTask.IsCompleted, result.IsCompleted);
        }

        [Fact]
        public async Task GetUserResourceRoleWithGetFirstOrDefaultByFilter_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            var userRole = new UserResourceRole
            {
                CreationDate = new DateTime(),
                ResourceId = resourceId,
                ResourceType = Shared.Enums.ResourceType.Case,
                Role = new Role { CreationDate = new DateTime(), Id = caseAtendeeRoleId, Name = RoleName.DepositionAttendee },
                RoleId = caseAtendeeRoleId,
                User = new User
                {
                    Id = userId,
                    CreationDate = new DateTime(),
                    FirstName = "FirstNameUser1",
                    LastName = "LastNameUser1",
                    EmailAddress = "test@test.com",
                    Password = "123456",
                    PhoneNumber = "1234567890",
                    IsAdmin = false,
                },
                UserId = userId,
                Id = caseAtendeeRoleId
            };

            // act
            var result = await rolePermissionResult.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<UserResourceRole, bool>>>(), It.IsAny<string[]>());

            // assert
            Assert.NotNull(result);
            Assert.Equal(result.ToString(), userRole.ToString());
        }
    }
}
