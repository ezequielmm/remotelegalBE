using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentResults;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PrecisionReporters.Platform.Api.Controllers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using PrecisionReporters.Platform.UnitTests.Utils;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Api.Controllers
{
    public class UsersControllerTest
    {
        private readonly IMapper<User, UserDto, CreateUserDto> _userMapper;
        private readonly Mock<IUserService> _userService;
        private readonly Mock<IDepositionService> _depositionService;
        private readonly UsersController _classUnderTest;

        public UsersControllerTest()
        {
            _userService = new Mock<IUserService>();
            _depositionService = new Mock<IDepositionService>();
            _userMapper = new UserMapper();
            _classUnderTest = new UsersController(_userService.Object, _depositionService.Object, _userMapper);
        }

        [Fact]
        public async Task SignUpAsync_ReturnOkAndUserDto()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                EmailAddress = "TestEmail@PascalCase.Com",
                FirstName = "First",
                LastName = "Last",
                PhoneNumber = "5555555555",
                CompanyAddress = "Company Address",
                CompanyName = "Mock & Co"
            };
            var user = UserFactory.GetUserByGivenId(Guid.NewGuid());
            _userService
                .Setup(mock => mock.SignUpAsync(It.IsAny<User>()))
                .ReturnsAsync(Result.Ok(user));

            _depositionService
                .Setup(mock => mock.UpdateParticipantOnExistingDepositions(It.IsAny<User>()))
                .ReturnsAsync(Result.Ok(new List<Deposition>()));

            // Act
            var result = await _classUnderTest.SignUpAsync(createUserDto);

            // Assert
            Assert.NotNull(result);
            var objectResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<UserDto>(objectResult.Value);
            _userService.Verify(mock => mock.SignUpAsync(It.IsAny<User>()), Times.Once);
            _depositionService.Verify(mock => mock.UpdateParticipantOnExistingDepositions(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task SignUpAsync_ReturnError_WhenSignUpFails()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                EmailAddress = "TestEmail@PascalCase.Com",
                FirstName = "First",
                LastName = "Last",
                PhoneNumber = "5555555555",
                CompanyAddress = "Company Address",
                CompanyName = "Mock & Co"
            };
            _userService
                .Setup(mock => mock.SignUpAsync(It.IsAny<User>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.SignUpAsync(createUserDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _userService.Verify(mock => mock.SignUpAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task VerifyUserAsync_ReturnOkAndResultDto()
        {
            // Arrange
            var verifyUserRequest = new VerifyUseRequestDto { VerificationHash = Guid.NewGuid() };
            _userService
                .Setup(mock => mock.VerifyUser(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok());

            // Act
            var result = await _classUnderTest.VerifyUserAsync(verifyUserRequest);

            // Assert
            Assert.NotNull(result);
            var objectResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<ResultDto>(objectResult.Value);
            _userService.Verify(mock => mock.VerifyUser(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_ReturnOkAndResultDto()
        {
            // Arrange
            var resendEmailRequestDto = new ResendEmailRequestDto { EmailAddress = "mock@mail.com" };
            _userService
                .Setup(mock => mock.ResendVerificationEmailAsync(It.IsAny<string>()));

            // Act
            var result = await _classUnderTest.ResendVerificationEmailAsync(resendEmailRequestDto);

            // Assert
            Assert.NotNull(result);
            var objectResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<ResultDto>(objectResult.Value);
            _userService.Verify(mock => mock.ResendVerificationEmailAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUser_ReturnOkAndUserDto()
        {
            // Arrange
            var user = UserFactory.GetUserByGivenId(Guid.NewGuid());
            _userService
                .Setup(mock => mock.GetCurrentUserAsync())
                .ReturnsAsync(user);

            // Act
            var result = await _classUnderTest.GetCurrentUser();

            // Assert
            Assert.NotNull(result);
            var objectResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<UserDto>(objectResult.Value);
            _userService.Verify(mock => mock.GetCurrentUserAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUser_ReturnNotFound()
        {
            // Arrange
            _userService
                .Setup(mock => mock.GetCurrentUserAsync())
                .ReturnsAsync(It.IsAny<User>());

            // Act
            var result = await _classUnderTest.GetCurrentUser();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<NotFoundResult>(result.Result);
            _userService.Verify(mock => mock.GetCurrentUserAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentAdminUser_ReturnOkAndUserDto()
        {
            // Arrange
            var user = UserFactory.GetUserByGivenId(Guid.NewGuid());
            user.IsAdmin = true;
            _userService
                .Setup(mock => mock.GetCurrentUserAsync())
                .ReturnsAsync(user);

            // Act
            var result = await _classUnderTest.GetCurrentAdminUser();

            // Assert
            Assert.NotNull(result);
            var objectResult = Assert.IsType<OkObjectResult>(result.Result);
            var okResult = Assert.IsType<UserDto>(objectResult.Value);
            Assert.True(okResult.IsAdmin);
            _userService.Verify(mock => mock.GetCurrentUserAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentAdminUser_ReturnNotFound()
        {
            // Arrange
            _userService
                .Setup(mock => mock.GetCurrentUserAsync())
                .ReturnsAsync(It.IsAny<User>());

            // Act
            var result = await _classUnderTest.GetCurrentAdminUser();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<NotFoundResult>(result.Result);
            _userService.Verify(mock => mock.GetCurrentUserAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentAdminUser_ReturnsForbidden()
        {
            // Arrange
            var user = UserFactory.GetUserByGivenId(Guid.NewGuid());
            _userService
                .Setup(mock => mock.GetCurrentUserAsync())
                .ReturnsAsync(user);

            // Act
            var result = await _classUnderTest.GetCurrentAdminUser();

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.Forbidden, errorResult.StatusCode);
            _userService.Verify(mock => mock.GetCurrentUserAsync(), Times.Once);
        }

        [Fact]
        public async Task ForgotPassword_ReturnOk()
        {
            // Arrange
            _userService
                .Setup(mock => mock.ForgotPassword(It.IsAny<ForgotPasswordDto>()))
                .ReturnsAsync(Result.Ok);

            // Act
            var result = await _classUnderTest.ForgotPassword(It.IsAny<ForgotPasswordDto>());

            // Assert
            Assert.NotNull(result);
            var objectResult = Assert.IsType<OkObjectResult>(result.Result);
            var okResult = Assert.IsType<bool>(objectResult.Value);
            Assert.True(okResult);
            _userService.Verify(mock => mock.ForgotPassword(It.IsAny<ForgotPasswordDto>()), Times.Once);
        }

        [Fact]
        public async Task ForgotPassword_ReturnError_WhenForgotPasswordFails()
        {
            // Arrange
            _userService
                .Setup(mock => mock.ForgotPassword(It.IsAny<ForgotPasswordDto>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.ForgotPassword(It.IsAny<ForgotPasswordDto>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _userService.Verify(mock => mock.ForgotPassword(It.IsAny<ForgotPasswordDto>()), Times.Once);
        }

        [Fact]
        public async Task VerifyForgotPassword_ReturnOkAndVerifyForgotPasswordOutputDto()
        {
            // Arrange
            var verifyForgotPasswordDto = new VerifyForgotPasswordDto { VerificationHash = Guid.NewGuid() };
            _userService
                .Setup(mock => mock.VerifyForgotPassword(It.IsAny<VerifyForgotPasswordDto>()))
                .ReturnsAsync(Result.Ok("mockVerification"));

            // Act
            var result = await _classUnderTest.VerifyForgotPassword(verifyForgotPasswordDto);

            // Assert
            Assert.NotNull(result);
            var objectResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<VerifyForgotPasswordOutputDto>(objectResult.Value);
            _userService.Verify(mock => mock.VerifyForgotPassword(It.IsAny<VerifyForgotPasswordDto>()), Times.Once);
        }

        [Fact]
        public async Task VerifyForgotPassword_ReturnError_WhenVerifyForgotPasswordFails()
        {
            // Arrange
            _userService
                .Setup(mock => mock.VerifyForgotPassword(It.IsAny<VerifyForgotPasswordDto>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.VerifyForgotPassword(It.IsAny<VerifyForgotPasswordDto>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _userService.Verify(mock => mock.VerifyForgotPassword(It.IsAny<VerifyForgotPasswordDto>()), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_ReturnOk()
        {
            // Arrange
            var resetPasswordDto = new ResetPasswordDto { Password = "Password123", VerificationHash = Guid.NewGuid() };
            _userService
                .Setup(mock => mock.ResetPassword(It.IsAny<ResetPasswordDto>()))
                .ReturnsAsync(Result.Ok);

            // Act
            var result = await _classUnderTest.ResetPassword(resetPasswordDto);

            // Assert
            Assert.NotNull(result);
            var objectResult = Assert.IsType<OkObjectResult>(result.Result);
            var okResult = Assert.IsType<bool>(objectResult.Value);
            Assert.True(okResult);
            _userService.Verify(mock => mock.ResetPassword(It.IsAny<ResetPasswordDto>()), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_ReturnError_WhenResetPasswordFails()
        {
            // Arrange
            _userService
                .Setup(mock => mock.ResetPassword(It.IsAny<ResetPasswordDto>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.ResetPassword(It.IsAny<ResetPasswordDto>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _userService.Verify(mock => mock.ResetPassword(It.IsAny<ResetPasswordDto>()), Times.Once);
        }

        [Fact]
        public async Task GetUsers_ReturnsOkAndUserFilterResponseDto()
        {
            // Arrange
            var userFilterDto = new UserFilterDto { Page = 1, PageSize = 10 };
            var user = UserFactory.GetUserByGivenId(Guid.NewGuid());
            user.IsAdmin = true;
            _userService
                .Setup(mock => mock.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _userService
                .Setup(mock => mock.GetUsersByFilter(It.IsAny<UserFilterDto>()))
                .ReturnsAsync(Result.Ok(new UserFilterResponseDto()));

            // Act
            var result = await _classUnderTest.GetUsers(userFilterDto);

            // Assert
            Assert.NotNull(result);
            var objectResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<UserFilterResponseDto>(objectResult.Value);
            _userService.Verify(mock => mock.GetCurrentUserAsync(), Times.Once);
            _userService.Verify(mock => mock.GetUsersByFilter(It.IsAny<UserFilterDto>()), Times.Once);
        }

        [Fact]
        public async Task GetUsers_ReturnsNotFound()
        {
            // Arrange
            var userFilterDto = new UserFilterDto { Page = 1, PageSize = 10 };
            _userService
                .Setup(mock => mock.GetCurrentUserAsync())
                .ReturnsAsync(It.IsAny<User>());
            _userService
                .Setup(mock => mock.GetUsersByFilter(It.IsAny<UserFilterDto>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetUsers(userFilterDto);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<NotFoundResult>(result.Result);
            _userService.Verify(mock => mock.GetCurrentUserAsync(), Times.Once);
            _userService.Verify(mock => mock.GetUsersByFilter(It.IsAny<UserFilterDto>()), Times.Never);
        }

        [Fact]
        public async Task GetUsers_ReturnsForbidden()
        {
            // Arrange
            var userFilterDto = new UserFilterDto { Page = 1, PageSize = 10 };
            var user = UserFactory.GetUserByGivenId(Guid.NewGuid());
            _userService
                .Setup(mock => mock.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _userService
                .Setup(mock => mock.GetUsersByFilter(It.IsAny<UserFilterDto>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetUsers(userFilterDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.Forbidden, errorResult.StatusCode);
            _userService.Verify(mock => mock.GetCurrentUserAsync(), Times.Once);
            _userService.Verify(mock => mock.GetUsersByFilter(It.IsAny<UserFilterDto>()), Times.Never);
        }

        [Fact]
        public async Task GetUsers_ReturnsError_WhenGetUsersByFilterFails()
        {
            // Arrange
            var userFilterDto = new UserFilterDto { Page = 1, PageSize = 10 };
            var user = UserFactory.GetUserByGivenId(Guid.NewGuid());
            user.IsAdmin = true;
            _userService
                .Setup(mock => mock.GetCurrentUserAsync())
                .ReturnsAsync(user);
            _userService
                .Setup(mock => mock.GetUsersByFilter(It.IsAny<UserFilterDto>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetUsers(userFilterDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _userService.Verify(mock => mock.GetCurrentUserAsync(), Times.Once);
            _userService.Verify(mock => mock.GetUsersByFilter(It.IsAny<UserFilterDto>()), Times.Once);
        }
    }
}