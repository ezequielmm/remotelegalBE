using Amazon.CognitoIdentityProvider.Model;
using Amazon.SimpleEmail.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Services
{
    public class UserServiceTests
    {
        private readonly UrlPathConfiguration _urlPathConfiguration;
        private readonly EmailConfiguration _emailConfiguration;    

        public UserServiceTests()
        {
            _urlPathConfiguration = ConfigurationFactory.GetUrlPathConfiguration();
            _emailConfiguration = ConfigurationFactory.GetEmailConfiguration();
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
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string>())).ReturnsAsync((User)null);
            userRepositoryMock.Setup(x => x.Create(It.IsAny<User>())).ReturnsAsync(user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.CreateVerifyUser(It.IsAny<VerifyUser>())).ReturnsAsync(verifyUser);
            var cognitoServiceMock = new Mock<ICognitoService>();
            cognitoServiceMock.Setup(x => x.CreateAsync(It.IsAny<User>())).Verifiable();

            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailTemplateInfo>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<List<string>>()))
                .ReturnsAsync(new SendRawEmailResponse());
            var service = InitializeService(userRepository: userRepositoryMock, cognitoService: cognitoServiceMock, awsEmailService: awsEmailServiceMock, verifyUserService: verifyUserServiceMock);

            // Act
            var result = await service.SignUpAsync(user);

            //Assert
            userRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string>()), Times.Once);
            userRepositoryMock.Verify(x => x.Create(It.Is<User>(a => a == user)), Times.Once);
            verifyUserServiceMock.Verify(x => x.CreateVerifyUser(It.IsAny<VerifyUser>()), Times.Once);
            cognitoServiceMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);
            awsEmailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<EmailTemplateInfo>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<List<string>>()), Times.Once);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public async Task SignUpAsync_ShouldThrow_ArgumentException_WhenEmailAlreadyExcist()
        {
            // Arrange           
            var email = "User1@TestMail.com";
            var errorMessage = $"User already exist: {email}";
            var id = Guid.NewGuid();
            var user = UserFactory.GetUserByGivenIdAndEmail(id, email);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string>())).ReturnsAsync(user);

            var service = InitializeService(userRepository: userRepositoryMock);

            //Assert
            var ex = await Assert.ThrowsAsync<UserAlreadyExistException>(() => service.SignUpAsync(user));
            Assert.Equal(errorMessage, ex.Message);
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
            var expectedErrorMessage = "Verification Code is already used or out of date";
            var id = Guid.NewGuid();
            var user = UserFactory.GetUserByGivenId(id);

            var dateNow = DateTime.UtcNow.AddHours(-24);
            var verifyUser = VerifyUserFactory.GetVerifyUserByGivenId(id, dateNow, user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserById(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            var logMock = new Mock<ILogger<UserService>>();
            var service = InitializeService(verifyUserService: verifyUserServiceMock, log: logMock);

            // Acr
            var result = await service.VerifyUser(verifyUser.Id);

            // Assert
            verifyUserServiceMock.Verify(x => x.GetVerifyUserById(It.IsAny<Guid>()), Times.Once);
            Assert.Single(logMock.Invocations);
            var loggerInvocation = logMock.Invocations[0];
            Assert.Equal(LogLevel.Warning, loggerInvocation.Arguments[0]);
            Assert.Equal(expectedErrorMessage, loggerInvocation.Arguments[2].ToString());
            Assert.Null(result);
        }

        [Fact]
        public async Task VerifyUser_Should_LogWarning_WhenIsUsedIsTrue()
        {
            // Arrange
            var expectedErrorMessage = "Verification Code is already used or out of date";
            var id = Guid.NewGuid();
            var user = UserFactory.GetUserByGivenId(id);

            var dateNow = DateTime.UtcNow;
            var verifyUser = VerifyUserFactory.GetUsedVerifyUserByGivenId(id, dateNow, user);

            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserById(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            var logMock = new Mock<ILogger<UserService>>();
            var service = InitializeService(verifyUserService: verifyUserServiceMock, log: logMock);

            // Acr
            var result = await service.VerifyUser(verifyUser.Id);

            // Assert
            verifyUserServiceMock.Verify(x => x.GetVerifyUserById(It.IsAny<Guid>()), Times.Once);
            Assert.Single(logMock.Invocations);
            var loggerInvocation = logMock.Invocations[0];
            Assert.Equal(LogLevel.Warning, loggerInvocation.Arguments[0]);
            Assert.Equal(expectedErrorMessage, loggerInvocation.Arguments[2].ToString());
            Assert.Null(result);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_ShouldCall_AwsEmailService_SendEmailAsync()
        {
            var email = "User1@TestMail.com";
            var user = UserFactory.GetUserByGivenEmail(email);
            var verifyUser = VerifyUserFactory.GetVerifyUserByGivenId(Guid.NewGuid(), DateTime.Now, user);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string>())).ReturnsAsync(user);
            var verifyUserServiceMock = new Mock<IVerifyUserService>();
            verifyUserServiceMock.Setup(x => x.GetVerifyUserByUserId(It.IsAny<Guid>())).ReturnsAsync(verifyUser);
            var awsEmailServiceMock = new Mock<IAwsEmailService>();
            awsEmailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<EmailTemplateInfo>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<List<string>>())).Verifiable();
            var service = InitializeService(userRepository: userRepositoryMock, verifyUserService: verifyUserServiceMock, awsEmailService: awsEmailServiceMock);

            // Act
            await service.ResendVerificationEmailAsync(email);

            // Assert
            userRepositoryMock.Verify(x => x.GetFirstOrDefaultByFilter(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string>()), Times.Once);
            verifyUserServiceMock.Verify(x => x.GetVerifyUserByUserId(It.Is<Guid>((a) => a == user.Id)), Times.Once);
            awsEmailServiceMock.Verify(x =>
                x.SendEmailAsync(It.IsAny<EmailTemplateInfo>(), It.Is<string>(a => a == email), It.IsAny<List<string>>(), It.IsAny<List<string>>()),
                Times.Once);
        }

        private UserService InitializeService(
            Mock<ILogger<UserService>> log = null,
            Mock<IUserRepository> userRepository = null,
            Mock<ICognitoService> cognitoService = null,
            Mock<IAwsEmailService> awsEmailService = null,
            Mock<IVerifyUserService> verifyUserService = null)
        {

            var logMock = log ?? new Mock<ILogger<UserService>>();
            var userRepositoryMock = userRepository ?? new Mock<IUserRepository>();
            var cognitoServiceMock = cognitoService ?? new Mock<ICognitoService>();
            var awsEmailServiceMock = awsEmailService ?? new Mock<IAwsEmailService>();
            var verifyUserServiceMock = verifyUserService ?? new Mock<IVerifyUserService>();
            var urlPathConfigurationMock = new Mock<IOptions<UrlPathConfiguration>>();
            urlPathConfigurationMock.Setup(x => x.Value).Returns(_urlPathConfiguration);
            var emailConfigurationMock = new Mock<IOptions<EmailConfiguration>>();
            emailConfigurationMock.Setup(x => x.Value).Returns(_emailConfiguration);   

            return new UserService(
                logMock.Object,
                userRepositoryMock.Object,
                cognitoServiceMock.Object,
                awsEmailServiceMock.Object,
                verifyUserServiceMock.Object,
                urlPathConfigurationMock.Object,
                emailConfigurationMock.Object
                );
        }
    }
}
