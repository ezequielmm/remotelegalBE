using Amazon.CognitoIdentityProvider.Model;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
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

        public UserServiceTests()
        {
            _urlPathConfiguration = ConfigurationFactory.GetUrlPathConfiguration();
            _verificationLinkConfiguration = ConfigurationFactory.GetVerificationLinkConfiguration();
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
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>())).ReturnsAsync((User)null);
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
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>())).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock, awsEmailService: awsEmailServiceMock, verifyUserService: verifyUserServiceMock, transactionHandler: transactionHandlerMock);

            // Act
            var result = await service.SignUpAsync(user);

            //Assert
            userRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>()), Times.Once);
            userRepositoryMock.Verify(x => x.Create(It.Is<User>(a => a == user)), Times.Once);
            verifyUserServiceMock.Verify(x => x.CreateVerifyUser(It.IsAny<VerifyUser>()), Times.Once);
            cognitoServiceMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);
            transactionHandlerMock.Verify(x => x.RunAsync(It.IsAny<Func<Task>>()), Times.Once);
            awsEmailServiceMock.Verify(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>()), Times.Once);

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
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(user);

            // Act
            var service = InitializeService(userRepository: userRepositoryMock);

            // Assert
            var result = await service.SignUpAsync(user);
            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ResourceConflictError>());
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
            cognitoServiceMock.Setup(x => x.ConfirmUserAsync(It.IsAny<string>())).ReturnsAsync(admmConfirmSignUpResponse);

            var service = InitializeService(verifyUserService: verifyUserServiceMock, cognitoService: cognitoServiceMock);

            // Act
            var result = await service.VerifyUser(id);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<VerifyUser>(result);

        }

        [Fact]
        public async Task VerifyUser_Should_LogWarning_WhenOlderThan24hours()
        {
            // Arrange
            var expectedErrorMessage = ApplicationConstants.VerificationCodeException;
            var id = Guid.NewGuid();
            var user = UserFactory.GetUserByGivenId(id);

            var expirationTime = int.Parse(_verificationLinkConfiguration.ExpirationTime);
            var dateNow = DateTime.UtcNow.AddHours(-expirationTime);
            var verifyUser = VerifyUserFactory.GetVerifyUserByGivenId(id, dateNow, user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserById(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            var logMock = new Mock<ILogger<UserService>>();
            var service = InitializeService(verifyUserService: verifyUserServiceMock, log: logMock);

            // Act
            var ex = await Assert.ThrowsAsync<HashExpiredOrAlreadyUsedException>(async () => await service.VerifyUser(verifyUser.Id));

            // Assert
            verifyUserServiceMock.Verify(x => x.GetVerifyUserById(It.IsAny<Guid>()), Times.Once);
            Assert.Single(logMock.Invocations);
            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Fact]
        public async Task VerifyUser_Should_LogWarning_WhenIsUsedIsTrue()
        {
            // Arrange
            var expectedErrorMessage = ApplicationConstants.VerificationCodeException;
            var id = Guid.NewGuid();
            var user = UserFactory.GetUserByGivenId(id);

            var dateNow = DateTime.UtcNow;
            var verifyUser = VerifyUserFactory.GetUsedVerifyUserByGivenId(id, dateNow, user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserById(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            var logMock = new Mock<ILogger<UserService>>();
            var service = InitializeService(verifyUserService: verifyUserServiceMock, log: logMock);

            // Act
            var ex = await Assert.ThrowsAsync<HashExpiredOrAlreadyUsedException>(async () => await service.VerifyUser(verifyUser.Id));

            // Assert
            verifyUserServiceMock.Verify(x => x.GetVerifyUserById(It.IsAny<Guid>()), Times.Once);
            Assert.Single(logMock.Invocations);
            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_ShouldCall_AwsEmailService_SendEmailAsync()
        {
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenEmail(email);
            var verifyUser = VerifyUserFactory.GetVerifyUserByGivenId(Guid.NewGuid(), DateTime.Now, user);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>())).ReturnsAsync(user);
            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserByUserId(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>())).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, verifyUserService: verifyUserServiceMock, awsEmailService: awsEmailServiceMock);

            // Act
            await service.ResendVerificationEmailAsync(email);

            // Assert
            userRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string[]>()), Times.Once);
            verifyUserServiceMock.Verify(x => x.GetVerifyUserByUserId(It.Is<Guid>((a) => a == user.Id)), Times.Once);
            awsEmailServiceMock.Verify(x => x.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>()), Times.Once);
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
        public async Task AddGuestUser_ShouldCreateNewUser_IfUserDoesNotExist()
        {
            // Arrange
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenEmail(email);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync((User)null);

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
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);

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
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);

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
            var result = service.RemoveGuestParticipants(participants);

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
            var result = service.RemoveGuestParticipants(participants);

            // Assert
            cognitoServiceMock.Verify(x => x.DeleteUserAsync(It.IsAny<User>()), Times.Never);
        }

        private UserService InitializeService(
            Mock<ILogger<UserService>> log = null,
            Mock<IUserRepository> userRepository = null,
            Mock<ICognitoService> cognitoService = null,
            Mock<IAwsEmailService> awsEmailService = null,
            Mock<IVerifyUserService> verifyUserService = null,
            Mock<ITransactionHandler> transactionHandler = null)
        {

            var logMock = log ?? new Mock<ILogger<UserService>>();
            var userRepositoryMock = userRepository ?? new Mock<IUserRepository>();
            var cognitoServiceMock = cognitoService ?? new Mock<ICognitoService>();
            var awsEmailServiceMock = awsEmailService ?? new Mock<IAwsEmailService>();
            var verifyUserServiceMock = verifyUserService ?? new Mock<IVerifyUserService>();
            var transactionHandlerMock = transactionHandler ?? new Mock<ITransactionHandler>();
            var urlPathConfigurationMock = new Mock<IOptions<UrlPathConfiguration>>();
            var verificationLinkConfigurationMock = new Mock<IOptions<VerificationLinkConfiguration>>();
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
                null);
        }
    }
}
