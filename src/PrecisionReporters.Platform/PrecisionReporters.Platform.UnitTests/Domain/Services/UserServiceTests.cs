using Amazon.CognitoIdentityProvider.Model;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Enums;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Errors;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class UserServiceTests
    {
        private readonly UrlPathConfiguration _urlPathConfiguration;
        private readonly VerificationLinkConfiguration _verificationLinkConfiguration;
        private readonly EmailTemplateNames _emailTemplateNames;

        public UserServiceTests()
        {
            _urlPathConfiguration = ConfigurationFactory.GetUrlPathConfiguration();
            _verificationLinkConfiguration = ConfigurationFactory.GetVerificationLinkConfiguration();
            _emailTemplateNames = new EmailTemplateNames { VerificationEmail = "TestEmailTemplate", ForgotPasswordEmail = "TestEmailTemplate" };
        }

        [Fact]
        public async Task SignUpAsync_ShouldReturn_NewUser()
        {
            // Arrange            
            var id = Guid.NewGuid();
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenIdAndEmail(id, email);
            var verifyUser = VerifyUserFactory.GetVerifyUser(user);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync((User)null);
            userRepositoryMock.Setup(x => x.Create(It.IsAny<User>())).ReturnsAsync(user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.CreateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(verifyUser);
            var cognitoServiceMock = new Mock<ICognitoService>();
            cognitoServiceMock.Setup(x => x.CreateAsync(It.IsAny<User>())).Verifiable();

            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock, awsEmailService: awsEmailServiceMock, verifyUserService: verifyUserServiceMock, transactionHandler: transactionHandlerMock);

            // Act
            var result = await service.SignUpAsync(user);

            //Assert
            userRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            userRepositoryMock.Verify(x => x.Create(It.Is<User>(a => a == user)), Times.Once);
            verifyUserServiceMock.Verify(x => x.CreateVerifyUser(It.IsAny<VerifyUser>()), Times.Once);
            cognitoServiceMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);
            transactionHandlerMock.Verify(x => x.RunAsync(It.IsAny<Func<Task>>()), Times.Once);
            awsEmailServiceMock.Verify(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null), Times.Once);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(id, result.Value.Id);
        }

        [Fact]
        public async Task SignUpAsync_ShouldGiveConflictError_WhenEmailAlreadyExists()
        {
            // Arrange           
            var email = "User1@TestMail.com";
            var id = Guid.NewGuid();
            var user = UserFactory.GetUserByGivenIdAndEmail(id, email);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(user);

            // Act
            var service = InitializeService(userRepository: userRepositoryMock);

            // Assert
            var result = await service.SignUpAsync(user);
            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ResourceConflictError>());
        }

        [Fact]
        public async Task SignUpAsync_ShouldUpdateGuestToUser_UpdatedUser()
        {
            // Arrange            
            var id = Guid.NewGuid();
            var email = "User1@TestMail.com";
            var user = UserFactory.GetGuestUserByGivenIdAndEmail(id, email);
            var verifyUser = VerifyUserFactory.GetVerifyUser(user);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.Update(It.IsAny<User>())).ReturnsAsync(user);

            var cognitoServiceMock = new Mock<ICognitoService>();
            cognitoServiceMock.Setup(x => x.CheckUserExists(It.IsAny<string>())).ReturnsAsync(Result.Ok());
            cognitoServiceMock.Setup(x => x.CreateAsync(It.IsAny<User>())).Verifiable();

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.CreateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(verifyUser);

            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock, awsEmailService: awsEmailServiceMock, verifyUserService: verifyUserServiceMock, transactionHandler: transactionHandlerMock);

            // Act
            var result = await service.SignUpAsync(user);

            //Assert
            userRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            userRepositoryMock.Verify(x => x.Update(It.Is<User>(a => a == user)), Times.Once);
            verifyUserServiceMock.Verify(x => x.CreateVerifyUser(It.IsAny<VerifyUser>()), Times.Once);
            cognitoServiceMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);
            transactionHandlerMock.Verify(x => x.RunAsync(It.IsAny<Func<Task>>()), Times.Once);
            awsEmailServiceMock.Verify(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null), Times.Once);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(id, result.Value.Id);
        }

        [Fact]
        public async Task VerifyUser_ShouldReturn_VerifyUser_WithIsUsedAsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenEmail(email);

            var dateNow = DateTime.UtcNow;
            var verifyUser = VerifyUserFactory.GetVerifyUserByGivenId(id, dateNow, user);

            var usedVerifyUser = VerifyUserFactory.GetVerifyUserByGivenId(id, dateNow, user);

            var admmConfirmSignUpResponse = new AdminConfirmSignUpResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            };

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserById(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            verifyUserServiceMock.Setup(x => x.UpdateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(usedVerifyUser);
            var cognitoServiceMock = new Mock<ICognitoService>();

            var service = InitializeService(verifyUserService: verifyUserServiceMock, cognitoService: cognitoServiceMock);

            // Act
            var result = await service.VerifyUser(id);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Result<VerifyUser>>(result);

        }

        [Fact]
        public async Task VerifyUser_Should_LogWarning_WhenOlderThanOneMonth()
        {
            // Arrange
            var id = Guid.NewGuid();
            var user = UserFactory.GetUserByGivenId(id);

            var expirationTime = int.Parse(ConfigurationFactory.GetVerificationLinkConfiguration().ExpirationTime);
            var dateNow = DateTime.UtcNow.AddHours(-expirationTime);
            var verifyUser = VerifyUserFactory.GetVerifyUserByGivenId(id, dateNow, user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserById(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            var logMock = new Mock<ILogger<UserService>>();
            var service = InitializeService(verifyUserService: verifyUserServiceMock, log: logMock);

            // Act
            var result = await service.VerifyUser(verifyUser.Id);

            // Assert
            verifyUserServiceMock.Verify(x => x.GetVerifyUserById(It.IsAny<Guid>()), Times.Once);
            Assert.Single(logMock.Invocations);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task VerifyUser_Should_LogWarning_WhenIsUsedIsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            var user = UserFactory.GetUserByGivenId(id);

            var dateNow = DateTime.UtcNow;
            var verifyUser = VerifyUserFactory.GetUsedVerifyUserByGivenId(id, dateNow, user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserById(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            var logMock = new Mock<ILogger<UserService>>();
            var service = InitializeService(verifyUserService: verifyUserServiceMock, log: logMock);

            // Act
            var result = await service.VerifyUser(verifyUser.Id);

            // Assert
            verifyUserServiceMock.Verify(x => x.GetVerifyUserById(It.IsAny<Guid>()), Times.Once);
            Assert.Single(logMock.Invocations);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_ShouldCall_AwsEmailService_SendEmailAsync()
        {
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenEmail(email);
            var verifyUser = VerifyUserFactory.GetVerifyUserByGivenId(Guid.NewGuid(), DateTime.Now, user);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(user);
            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserByUserId(It.IsAny<Guid>(), null)).ReturnsAsync(verifyUser);
            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, verifyUserService: verifyUserServiceMock, awsEmailService: awsEmailServiceMock);

            // Act
            await service.ResendVerificationEmailAsync(email);

            // Assert
            userRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            verifyUserServiceMock.Verify(x => x.GetVerifyUserByUserId(It.Is<Guid>((a) => a == user.Id), null), Times.Once);
            awsEmailServiceMock.Verify(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null), Times.Once);
        }
        
        [Fact]
        public async Task ResendVerificationEmailAsync_ShouldRefreshVerification_IfIsExpired()
        {
            var email = "User1@TestMail.com";
            var expiredMonth = DateTime.UtcNow.AddMonths(-1);
            var user = UserFactory.GetUserByGivenEmail(email);
            var expiredVerifyUser = VerifyUserFactory.GetVerifyUserByGivenId(Guid.NewGuid(), expiredMonth, user);
            var updatedVerifyUser = VerifyUserFactory.GetVerifyUserByGivenId(expiredVerifyUser.Id, DateTime.UtcNow, user);
            
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(user);
            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserByUserId(It.IsAny<Guid>(), null)).ReturnsAsync(expiredVerifyUser);
            verifyUserServiceMock.Setup(x => x.UpdateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(updatedVerifyUser);
            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, verifyUserService: verifyUserServiceMock, awsEmailService: awsEmailServiceMock);

            // Act
            await service.ResendVerificationEmailAsync(email);

            // Assert
            userRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            verifyUserServiceMock.Verify(x => x.GetVerifyUserByUserId(It.Is<Guid>((a) => a == user.Id), null), Times.Once);
            verifyUserServiceMock.Verify(x => x.UpdateVerifyUser(It.IsAny<VerifyUser>()), Times.Once);
            awsEmailServiceMock.Verify(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null), Times.Once);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_ShouldCreateANewOne_IfIsNotExist()
        {
            var email = "User1@TestMail.com";
            var expiredMonth = DateTime.UtcNow.AddMonths(-1);
            var user = UserFactory.GetUserByGivenEmail(email);
            var newVerifyUser = VerifyUserFactory.GetVerifyUserByGivenId(Guid.NewGuid(), DateTime.UtcNow, user);
            
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(user);
            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserByUserId(It.IsAny<Guid>(), null)).ReturnsAsync((VerifyUser)null);
            verifyUserServiceMock.Setup(x => x.CreateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(newVerifyUser);
            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, verifyUserService: verifyUserServiceMock, awsEmailService: awsEmailServiceMock);

            // Act
            await service.ResendVerificationEmailAsync(email);

            // Assert
            userRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            verifyUserServiceMock.Verify(x => x.GetVerifyUserByUserId(It.Is<Guid>((a) => a == user.Id), null), Times.Once);
            verifyUserServiceMock.Verify(x => x.CreateVerifyUser(It.IsAny<VerifyUser>()), Times.Once);
            awsEmailServiceMock.Verify(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null), Times.Once);
        }
        
        [Fact]
        public async Task GetUsersByFilter_ShouldReturnFailure_IfRepositoryReturnsNull()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(new List<User>());
            var service = InitializeService(userRepository: userRepositoryMock);

            // Act
            var result = await service.GetUsersByFilter();

            // Assert
            userRepositoryMock.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<List<User>>(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUsersByFilter_ShouldReturnSuccess_IfRepositoryReturnsAtLeastAUser()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(new List<User> { new User() });
            var service = InitializeService(userRepository: userRepositoryMock);

            // Act
            var result = await service.GetUsersByFilter();

            // Assert
            userRepositoryMock.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<List<User>>(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetUsersByFilter_ShouldReturnAllUsers_WhenFilterParameterIsNull()
        {
            var users = new List<User>();
            users.AddRange(UserFactory.GetUserList());
            var upcomingList = users.FindAll(x => x.IsGuest == false);
            var usersResult = new Tuple<int, IEnumerable<User>>(upcomingList.Count, upcomingList.AsQueryable());

            var filter = new UserFilterDto
            {
                SortDirection = SortDirection.Ascend,
                SortedField = UserSortField.Company,
                Page = 1,
                PageSize = 20
            };

            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();

            userRepositoryMock
                .Setup(mock => mock.GetByFilterPagination(
                   null,
                   It.IsAny<Func<IQueryable<User>,
                   IOrderedQueryable<User>>>(),
                   It.IsAny<string[]>(),
                   It.IsAny<int>(),
                   It.IsAny<int>()
                   ))
                .ReturnsAsync(usersResult);

            var service = InitializeService(userRepository: userRepositoryMock);

            // Act
            var result = await service.GetUsersByFilter(filter);

            // Assert
            Assert.NotNull(result);
            userRepositoryMock.Verify(mock => mock.GetByFilterPagination(
               null,
               It.IsAny<Func<IQueryable<User>,
               IOrderedQueryable<User>>>(),
               It.IsAny<string[]>(),
               It.IsAny<int>(),
               It.IsAny<int>()
                ), Times.Once);
        }

        [Fact]
        public async Task GetUsersByFilter_ShouldReturnAdmin_WhenFilterParameterIsNull()
        {
            var users = new List<User>();
            users.AddRange(UserFactory.GetUserList());
            var upcomingList = users.FindAll(x => x.IsAdmin == true);
            var usersResult = new Tuple<int, IEnumerable<User>>(upcomingList.Count, upcomingList.AsQueryable());

            var filter = new UserFilterDto
            {
                SortDirection = SortDirection.Ascend,
                SortedField = UserSortField.Company,
                Page = 1,
                PageSize = 20
            };

            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var userServiceMock = new Mock<IUserService>();

            userRepositoryMock
                .Setup(mock => mock.GetByFilterPagination(
                   null,
                   It.IsAny<Func<IQueryable<User>,
                   IOrderedQueryable<User>>>(),
                   It.IsAny<string[]>(),
                   It.IsAny<int>(),
                   It.IsAny<int>()
                   ))
                .ReturnsAsync(usersResult);

            userServiceMock.Setup(mock => mock.GetCurrentUserAsync()).ReturnsAsync(new User { IsAdmin = true });

            var service = InitializeService(userRepository: userRepositoryMock);

            // Act
            var result = await service.GetUsersByFilter(filter);

            // Assert
            Assert.NotNull(result);
            userRepositoryMock.Verify(mock => mock.GetByFilterPagination(
               null,
               It.IsAny<Func<IQueryable<User>,
               IOrderedQueryable<User>>>(),
               It.IsAny<string[]>(),
               It.IsAny<int>(),
               It.IsAny<int>()
                ), Times.Once);
        }

        [Fact]
        public async Task GetUsersByFilter_ShouldReturnOrderedUsersListByLastName_WhenSortDirectionIsAscendAndSortedFieldIsLastName()
        {
            // Arrange
            var sortedList = UserFactory.GetUserList().OrderBy(x => x.LastName).ThenBy(x => x.FirstName);
            var users = new List<User>();
            users.AddRange(sortedList);
            var upcomingList = users.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToList();
            var usersResult = new Tuple<int, IEnumerable<User>>(upcomingList.Count, upcomingList.AsQueryable());
            var userRepositoryMock = new Mock<IUserRepository>();

            userRepositoryMock
                .Setup(mock => mock.GetByFilterPagination(
                   It.IsAny<Expression<Func<User, bool>>>(),
                   It.IsAny<Func<IQueryable<User>,
                   IOrderedQueryable<User>>>(),
                   It.IsAny<string[]>(),
                   It.IsAny<int>(),
                   It.IsAny<int>()
                   ))
                .ReturnsAsync(usersResult);

            var filter = new UserFilterDto
            {
                SortDirection = SortDirection.Ascend,
                SortedField = UserSortField.LastName,
                Page = 1,
                PageSize = 20
            };

            var service = InitializeService(userRepository: userRepositoryMock);

            // Act
            var result = await service.GetUsersByFilter(filter);

            // Assert
            Assert.NotNull(result);
            userRepositoryMock.Verify(mock => mock.GetByFilterPagination(
               It.IsAny<Expression<Func<User, bool>>>(),
               It.IsAny<Func<IQueryable<User>,
               IOrderedQueryable<User>>>(),
               It.IsAny<string[]>(),
               It.IsAny<int>(),
               It.IsAny<int>()
                ), Times.Once);
            Assert.True(result.Value.Users.Any());
            Assert.True(result.Value.Users.Count == 2);
            Assert.True(result.Value.Total == 2);
        }

        [Fact]
        public async Task GetUsersByFilter_ShouldOrderByThen_WhenSortedFieldIsCompanyName()
        {
            // Arrange
            var sortedList = UserFactory.GetUserList().OrderBy(x => x.CompanyName).ThenBy(x => x.FirstName);
            var users = new List<User>();
            users.AddRange(sortedList);
            var upcomingList = users.OrderBy(x => x.CompanyName).ThenBy(x => x.FirstName).ToList();
            var usersResult = new Tuple<int, IEnumerable<User>>(upcomingList.Count, upcomingList.AsQueryable());
            var userRepositoryMock = new Mock<IUserRepository>();

            userRepositoryMock
                .Setup(mock => mock.GetByFilterPagination(
                   It.IsAny<Expression<Func<User, bool>>>(),
                   It.IsAny<Func<IQueryable<User>,
                   IOrderedQueryable<User>>>(),
                   It.IsAny<string[]>(),
                   It.IsAny<int>(),
                   It.IsAny<int>()
                   ))
                .ReturnsAsync(usersResult);

            var filter = new UserFilterDto
            {
                SortDirection = SortDirection.Ascend,
                SortedField = UserSortField.Company,
                Page = 1,
                PageSize = 20
            };

            var service = InitializeService(userRepository: userRepositoryMock);

            // Act
            var result = await service.GetUsersByFilter(filter);

            // Assert
            Assert.NotNull(result);
            userRepositoryMock.Verify(mock => mock.GetByFilterPagination(
               It.IsAny<Expression<Func<User, bool>>>(),
               It.IsAny<Func<IQueryable<User>,
               IOrderedQueryable<User>>>(),
               It.IsAny<string[]>(),
               It.IsAny<int>(),
               It.IsAny<int>()
                ), Times.Once);
            Assert.True(result.Value.Users.Any());
            Assert.True(result.Value.Users.Count == 2);
            Assert.True(result.Value.Total == 2);
        }

        [Fact]
        public async Task AddGuestUser_ShouldCreateNewUser_IfUserDoesNotExist()
        {
            // Arrange
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenEmail(email);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), null, It.IsAny<bool>())).ReturnsAsync((User)null);

            var cognitoServiceMock = new Mock<ICognitoService>();
            cognitoServiceMock.Setup(x => x.CheckUserExists(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error()));

            var transactionHandlerMock = new Mock<ITransactionHandler>();

            var loggerMock = new Mock<ILogger<UserService>>();

            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock
                , transactionHandler: transactionHandlerMock, _userLogger: loggerMock);

            // Act
            var result = await service.AddGuestUser(user);

            // Assert
            userRepositoryMock.Verify(x => x.Create(It.IsAny<User>()), Times.Once);
            cognitoServiceMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AddGuestUser_ShouldCreateOnlyCognitoUser_IfUserExistsInDB()
        {
            // Arrange
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenEmail(email);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), null, It.IsAny<bool>())).ReturnsAsync(user);

            var cognitoServiceMock = new Mock<ICognitoService>();
            cognitoServiceMock.Setup(x => x.CheckUserExists(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error()));

            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock
                , transactionHandler: transactionHandlerMock);

            // Act
            var result = await service.AddGuestUser(user);

            // Assert
            userRepositoryMock.Verify(x => x.Create(It.IsAny<User>()), Times.Never);
            cognitoServiceMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AddGuestUser_ShouldNotCreateAUser_IfUserExistsInDBAndCognito()
        {
            // Arrange
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenEmail(email);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), null, It.IsAny<bool>())).ReturnsAsync(user);

            var cognitoServiceMock = new Mock<ICognitoService>();
            cognitoServiceMock.Setup(x => x.CheckUserExists(It.IsAny<string>())).ReturnsAsync(Result.Ok());

            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock
                , transactionHandler: transactionHandlerMock);

            // Act
            var result = await service.AddGuestUser(user);

            // Assert
            userRepositoryMock.Verify(x => x.Create(It.IsAny<User>()), Times.Never);
            cognitoServiceMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RemoveGuestParticipants_ShouldRemoveOnlyGuestUsers()
        {
            var participantGuest = new Participant
            {
                Email = "participantGuest@guest.com",
                User = new User { IsGuest = true, EmailAddress = "participantGuest@guest.com" }
            };
            var participantUser = new Participant
            {
                Email = "participantUser@user.com",
                User = new User { IsGuest = false, EmailAddress = "participantUser@user.com" }
            };
            // Arrange
            var participants = new List<Participant>
            {
                participantGuest, participantUser
            };

            var cognitoServiceMock = new Mock<ICognitoService>();
            cognitoServiceMock.Setup(x => x.CheckUserExists(It.IsAny<string>())).ReturnsAsync(Result.Ok());
            cognitoServiceMock.Setup(x => x.DeleteUserAsync(It.IsAny<User>())).ReturnsAsync(Result.Ok());

            var service = InitializeService(cognitoService: cognitoServiceMock);

            // Act
            await service.RemoveGuestParticipants(participants);

            // Assert
            cognitoServiceMock.Verify(x => x.DeleteUserAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task RemoveGuestParticipants_ShouldNotCallDeleteUser_IfCognitoUserDoesNotExist()
        {
            var participantGuest = new Participant
            {
                Email = "participantGuest@guest.com",
                User = new User { IsGuest = true, EmailAddress = "participantGuest@guest.com" }
            };
            // Arrange
            var participants = new List<Participant>
            {
                participantGuest
            };

            var cognitoServiceMock = new Mock<ICognitoService>();
            cognitoServiceMock.Setup(x => x.CheckUserExists(It.IsAny<string>())).ReturnsAsync(Result.Fail(new Error()));
            cognitoServiceMock.Setup(x => x.DeleteUserAsync(It.IsAny<User>())).ReturnsAsync(Result.Ok());

            var service = InitializeService(cognitoService: cognitoServiceMock);

            // Act
            await service.RemoveGuestParticipants(participants);

            // Assert
            cognitoServiceMock.Verify(x => x.DeleteUserAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ForgotPassword_ShouldReturnOk_WhenTheUserExists()
        {
            // Arrange
            var dto = new ForgotPasswordDto { Email = "User1@TestMail.com" };
            var user = UserFactory.GetUserByGivenEmail(dto.Email);
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), null, It.IsAny<bool>())).ReturnsAsync(user);
            var verifyUser = VerifyUserFactory.GetVerifyForgotPassword(user);
            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.CreateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(verifyUser);
            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });
            var service = InitializeService(userRepository: userRepositoryMock, verifyUserService: verifyUserServiceMock, awsEmailService: awsEmailServiceMock, transactionHandler: transactionHandlerMock);

            // Act
            var result = await service.ForgotPassword(dto);

            // Assert
            Assert.True(result.IsSuccess);
            userRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), null, It.IsAny<bool>()), Times.Once);
            verifyUserServiceMock.Verify(x => x.CreateVerifyUser(It.IsAny<VerifyUser>()), Times.Once);
            awsEmailServiceMock.Verify(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null), Times.Once);
        }

        [Fact]
        public async Task ForgotPassword_ShouldReturnFail_WhenTheUserDoesNotExist()
        {
            // Arrange
            var dto = new ForgotPasswordDto { Email = "User1@TestMail.com" };
            var user = UserFactory.GetUserByGivenEmail(dto.Email);
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), null, It.IsAny<bool>())).ReturnsAsync((User)null);
            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var logMock = new Mock<ILogger<UserService>>();
            var service = InitializeService(awsEmailService: awsEmailServiceMock, log: logMock, userRepository: userRepositoryMock);

            // Act
            var result = await service.ForgotPassword(dto);

            // Assert
            userRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), null, It.IsAny<bool>()), Times.Once);
            awsEmailServiceMock.Verify(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null), Times.Never);
            Assert.Single(logMock.Invocations);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task VerifyForgotPassword_ShouldReturnOk_IfTheVerificationCodeIsValid()
        {
            // Arrange
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenEmail(email);
            var verifyUser = VerifyUserFactory.GetVerifyForgotPassword(user);
            var dto = new VerifyForgotPasswordDto { VerificationHash = verifyUser.Id };
            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserById(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            var service = InitializeService(verifyUserService: verifyUserServiceMock);

            // Act
            var result = await service.VerifyForgotPassword(dto);

            // Assert
            Assert.True(result.IsSuccess);
            verifyUserServiceMock.Verify(x => x.GetVerifyUserById(It.Is<Guid>((x) => x == verifyUser.Id)), Times.Once);
        }

        [Fact]
        public async Task VerifyForgotPassword_ShouldReturnFail_IfTheVerificationCodeIsNotValid()
        {
            // Arrange
            var verifyUserId = Guid.NewGuid();
            var dto = new VerifyForgotPasswordDto { VerificationHash = verifyUserId };
            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserById(It.IsAny<Guid>())).ReturnsAsync((VerifyUser)null);
            var service = InitializeService(verifyUserService: verifyUserServiceMock);

            // Act
            var result = await service.VerifyForgotPassword(dto);

            // Assert
            Assert.True(result.IsFailed);
            verifyUserServiceMock.Verify(x => x.GetVerifyUserById(It.Is<Guid>((x) => x == verifyUserId)), Times.Once);
        }

        [Fact]
        public async Task VerifyForgotPassword_ShouldReturnFail_WhenTheHashIsExpired()
        {
            // Arrange
            var id = Guid.NewGuid();
            var user = UserFactory.GetUserByGivenId(id);
            var expirationTime = int.Parse(ConfigurationFactory.GetVerificationLinkConfiguration().ExpirationTime);
            var dateNow = DateTime.UtcNow.AddHours(-expirationTime);
            var verifyUser = VerifyUserFactory.GetVerifyUserByGivenId(id, dateNow, user);
            var dto = new VerifyForgotPasswordDto { VerificationHash = verifyUser.Id };

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserById(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            var logMock = new Mock<ILogger<UserService>>();
            var service = InitializeService(verifyUserService: verifyUserServiceMock, log: logMock);

            // Act
            var result = await service.VerifyForgotPassword(dto);

            // Assert
            verifyUserServiceMock.Verify(x => x.GetVerifyUserById(It.IsAny<Guid>()), Times.Once);
            Assert.Single(logMock.Invocations);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task ResetPassword_ShouldUpdatePassword_WhenTransactionResultOk()
        {
            // Arrange
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenEmail(email);
            var verifyUser = VerifyUserFactory.GetVerifyForgotPassword(user);
            var dto = new ResetPasswordDto { VerificationHash = verifyUser.Id, Password = "Test123" };

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserById(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            verifyUserServiceMock.Setup(x => x.UpdateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(verifyUser);

            var cognitoServiceMock = new Mock<ICognitoService>();
            cognitoServiceMock.Setup(x => x.ResetPassword(It.IsAny<User>())).ReturnsAsync(Result.Ok());

            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            var service = InitializeService(cognitoService: cognitoServiceMock
                , transactionHandler: transactionHandlerMock, verifyUserService: verifyUserServiceMock);

            // Act
            var result = await service.ResetPassword(dto);

            // Assert
            cognitoServiceMock.Verify(x => x.ResetPassword(It.IsAny<User>()), Times.Once);
            verifyUserServiceMock.Verify(x => x.UpdateVerifyUser(It.IsAny<VerifyUser>()), Times.Once);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ResetPassword_ShouldReturnFail_WhenTransactionFails()
        {
            // Arrange
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenEmail(email);
            var verifyUser = VerifyUserFactory.GetVerifyForgotPassword(user);
            var dto = new ResetPasswordDto { VerificationHash = verifyUser.Id, Password = "Test123" };

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserById(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            verifyUserServiceMock.Setup(x => x.UpdateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(verifyUser);

            var cognitoServiceMock = new Mock<ICognitoService>();
            cognitoServiceMock.Setup(x => x.ResetPassword(It.IsAny<User>())).ReturnsAsync(Result.Fail(new Error()));

            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Fail(new Error());
                });

            var service = InitializeService(cognitoService: cognitoServiceMock
                , transactionHandler: transactionHandlerMock, verifyUserService: verifyUserServiceMock);

            // Act
            var result = await service.ResetPassword(dto);

            // Assert
            cognitoServiceMock.Verify(x => x.ResetPassword(It.IsAny<User>()), Times.Once);
            verifyUserServiceMock.Verify(x => x.UpdateVerifyUser(It.IsAny<VerifyUser>()), Times.Once);
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task GetCurrentUserAsync_ShouldReturnUser()
        {
            // arrange
            var userRepositoryMock = new Mock<IUserRepository>();

            userRepositoryMock
                .Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()))
                .ReturnsAsync(new User());

            // Arrange            
            var id = Guid.NewGuid();
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenIdAndEmail(id, email);
            var verifyUser = VerifyUserFactory.GetVerifyUser(user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.CreateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(verifyUser);
            var cognitoServiceMock = new Mock<ICognitoService>();
            cognitoServiceMock.Setup(x => x.CreateAsync(It.IsAny<User>())).Verifiable();

            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock, awsEmailService: awsEmailServiceMock, verifyUserService: verifyUserServiceMock, transactionHandler: transactionHandlerMock);

            // Act
            var result = await service.GetCurrentUserAsync();

            // assert
            userRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<User>(result);
        }

        [Fact]
        public async Task LoginGuestAsync_ShouldReturnGuestToken()
        {
            // arrange
            var cognitoServiceMock = new Mock<ICognitoService>();

            cognitoServiceMock
                .Setup(x => x.LoginGuestAsync(It.IsAny<User>()))
                .ReturnsAsync(Result.Ok(new GuestToken()));

            var id = Guid.NewGuid();
            var email = "User1@TestMail.com";
            var user = new User
            {
                Id = id,
                CreationDate = DateTime.Now,
                FirstName = "FirstNameUser1",
                LastName = "LastNameUser1",
                EmailAddress = email,
                Password = "123456",
                PhoneNumber = "1234567890",
                IsGuest = true
            };
            var verifyUser = VerifyUserFactory.GetVerifyUser(user);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.Update(It.IsAny<User>())).ReturnsAsync(user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.CreateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(verifyUser);

            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock, awsEmailService: awsEmailServiceMock, verifyUserService: verifyUserServiceMock, transactionHandler: transactionHandlerMock);

            // act
            var result = await service.LoginGuestAsync("User1@TestMail.com");

            // assert
            cognitoServiceMock.Verify(x => x.LoginGuestAsync(It.IsAny<User>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<GuestToken>>(result);
        }

        [Fact]
        public async Task LoginGuestAsync_ShouldFailWhenGuestIsFalse()
        {
            // arrange
            var cognitoServiceMock = new Mock<ICognitoService>();

            //var user = UserFactory.GetUserByGivenEmail("test@mock.com");

            cognitoServiceMock
                .Setup(x => x.LoginGuestAsync(It.IsAny<User>()))
                .ReturnsAsync(Result.Ok(new GuestToken()));

            var id = Guid.NewGuid();
            var email = "User1@TestMail.com";
            var user = new User
            {
                Id = id,
                CreationDate = DateTime.Now,
                FirstName = "FirstNameUser1",
                LastName = "LastNameUser1",
                EmailAddress = email,
                Password = "123456",
                PhoneNumber = "1234567890",
                IsGuest = false
            };
            var verifyUser = VerifyUserFactory.GetVerifyUser(user);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.Update(It.IsAny<User>())).ReturnsAsync(user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.CreateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(verifyUser);

            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock, awsEmailService: awsEmailServiceMock, verifyUserService: verifyUserServiceMock, transactionHandler: transactionHandlerMock);

            // act
            var result = await service.LoginGuestAsync("User1@TestMail.com");

            var expectedError = "Invalid user";

            // assert
            Assert.NotNull(result);
            Assert.IsType<Result<GuestToken>>(result);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message).Single());
        }

        [Fact]
        public async Task DisableUnverifiedParticipants_ShouldReturnParticipant()
        {
            // arrange
            var id = Guid.NewGuid();
            var email = "User1@TestMail.com";
            var user = new User
            {
                Id = id,
                CreationDate = DateTime.Now,
                FirstName = "FirstNameUser1",
                LastName = "LastNameUser1",
                EmailAddress = email,
                Password = "123456",
                PhoneNumber = "1234567890",
                IsGuest = false,
            };

            List<Participant> participants = new List<Participant>() {
                new Participant
                {
                    Id = Guid.NewGuid(),
                    Email = "participant@email.com",
                    Name = "Participant Email",
                    IsMuted = false,
                    DepositionId = Guid.NewGuid(),
                    User = UserFactory.GetUserByGivenEmail("participant@email.com")
                }
            };

            var cognitoServiceMock = new Mock<ICognitoService>();

            cognitoServiceMock
                .Setup(x => x.GetUserCognitoGroupList(It.IsAny<string>()))
                .ReturnsAsync(Result.Ok(new List<CognitoGroup>()));

            cognitoServiceMock
                .Setup(x => x.DisableUser(It.IsAny<User>()))
                .Verifiable();

            var verifyUser = VerifyUserFactory.GetVerifyUser(user);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.Update(It.IsAny<User>())).ReturnsAsync(user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.CreateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(verifyUser);

            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock, awsEmailService: awsEmailServiceMock, verifyUserService: verifyUserServiceMock, transactionHandler: transactionHandlerMock);
            
            // act
            try
            {
                await service.DisableUnverifiedParticipants(participants);
                Assert.True(true);
            }
            catch
            {
                Assert.True(false);
            }
        }

        [Fact]
        public async Task LoginUnverifiedAsync_ShouldReturnGuestTokenGuestFalse()
        {
            // arrange
            var cognitoServiceMock = new Mock<ICognitoService>();

            cognitoServiceMock
                .Setup(x => x.LoginUnVerifiedAsync(It.IsAny<User>()))
                .ReturnsAsync(Result.Ok(new GuestToken()));

            var id = Guid.NewGuid();
            var email = "User1@TestMail.com";
            var user = new User
            {
                Id = id,
                CreationDate = DateTime.Now,
                FirstName = "FirstNameUser1",
                LastName = "LastNameUser1",
                EmailAddress = email,
                Password = "123456",
                PhoneNumber = "1234567890",
                IsGuest = false
            };
            var verifyUser = VerifyUserFactory.GetVerifyUser(user);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.Update(It.IsAny<User>())).ReturnsAsync(user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.CreateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(verifyUser);

            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock, awsEmailService: awsEmailServiceMock, verifyUserService: verifyUserServiceMock, transactionHandler: transactionHandlerMock);

            // act
            var result = await service.LoginUnverifiedAsync(user);

            // assert
            cognitoServiceMock.Verify(x => x.LoginUnVerifiedAsync(It.IsAny<User>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<GuestToken>>(result);
        }

        [Fact]
        public async Task LoginUnverifiedAsync_ShouldReturnGuestTokenGuestTrue()
        {
            // arrange
            var cognitoServiceMock = new Mock<ICognitoService>();

            cognitoServiceMock
                .Setup(x => x.LoginUnVerifiedAsync(It.IsAny<User>()))
                .ReturnsAsync(Result.Ok(new GuestToken()));

            var id = Guid.NewGuid();
            var email = "User1@TestMail.com";
            var user = new User
            {
                Id = id,
                CreationDate = DateTime.Now,
                FirstName = "FirstNameUser1",
                LastName = "LastNameUser1",
                EmailAddress = email,
                Password = "123456",
                PhoneNumber = "1234567890",
                IsGuest = true
            };
            var verifyUser = VerifyUserFactory.GetVerifyUser(user);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.Update(It.IsAny<User>())).ReturnsAsync(user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.CreateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(verifyUser);

            var transactionHandlerMock = new Mock<ITransactionHandler>();
            transactionHandlerMock
                .Setup(x => x.RunAsync(It.IsAny<Func<Task>>()))
                .Returns(async (Func<Task> action) =>
                {
                    await action();
                    return Result.Ok();
                });

            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), null)).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock, awsEmailService: awsEmailServiceMock, verifyUserService: verifyUserServiceMock, transactionHandler: transactionHandlerMock);

            // act
            var result = await service.LoginUnverifiedAsync(user);

            var expectedError = "Invalid user";

            // assert
            Assert.NotNull(result);
            Assert.IsType<Result<GuestToken>>(result);
            Assert.Contains(expectedError, result.Errors.Select(e => e.Message).Single());
        }

        private UserService InitializeService(
            Mock<ILogger<UserService>> log = null,
            Mock<IUserRepository> userRepository = null,
            Mock<ICognitoService> cognitoService = null,
            Mock<IAwsEmailService> awsEmailService = null,
            Mock<IVerifyUserService> verifyUserService = null,
            Mock<ITransactionHandler> transactionHandler = null,
            Mock<IMapper<User, UserDto, CreateUserDto>> _userMapper = null,
            Mock<ILogger<UserService>> _userLogger = null)
        {

            var logMock = log ?? new Mock<ILogger<UserService>>();
            var userRepositoryMock = userRepository ?? new Mock<IUserRepository>();
            var cognitoServiceMock = cognitoService ?? new Mock<ICognitoService>();
            var awsEmailServiceMock = awsEmailService ?? new Mock<IAwsEmailService>();
            var verifyUserServiceMock = verifyUserService ?? new Mock<IVerifyUserService>();
            var transactionHandlerMock = transactionHandler ?? new Mock<ITransactionHandler>();
            var urlPathConfigurationMock = new Mock<IOptions<UrlPathConfiguration>>();
            var verificationLinkConfigurationMock = new Mock<IOptions<VerificationLinkConfiguration>>();
            var userMapperMock = _userMapper ?? new Mock<IMapper<User, UserDto, CreateUserDto>>();
            var userLogger = _userLogger ?? new Mock<ILogger<UserService>>();

            var _emailTemplateNamesMock = new Mock<IOptions<EmailTemplateNames>>();
            _emailTemplateNamesMock.Setup(x => x.Value).Returns(_emailTemplateNames);

            urlPathConfigurationMock.Setup(x => x.Value).Returns(_urlPathConfiguration);
            verificationLinkConfigurationMock.Setup(x => x.Value).Returns(_verificationLinkConfiguration);

            return new UserService(
                logMock.Object,
                userRepositoryMock.Object,
                cognitoServiceMock.Object,
                awsEmailServiceMock.Object,
                verifyUserServiceMock.Object,
                transactionHandlerMock.Object,
                urlPathConfigurationMock.Object,
                verificationLinkConfigurationMock.Object,
                null,
                userMapperMock.Object,
                userLogger.Object,
                _emailTemplateNamesMock.Object);
        }
    }
}
