using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PrecisionReporters.Platform.Api.Controllers;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.UnitTests.Utils;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Api.Controllers
{
    public class PermissionsControllerTest
    {
        private readonly Mock<IDepositionService> _depositionService;
        private readonly Mock<IPermissionService> _permissionService;
        private readonly Mock<IUserService> _userService;
        private readonly PermissionsController _permissionsController;
        public PermissionsControllerTest()
        {
            _depositionService = new Mock<IDepositionService>();
            _permissionService = new Mock<IPermissionService>();
            _userService = new Mock<IUserService>();
            _permissionsController = new PermissionsController(_depositionService.Object, _permissionService.Object, _userService.Object);
        }

        [Fact]
        public async Task GetDepositionPermissionsForParticipant_NonAdmin_ReturnsOk()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();

            var depositionId = Guid.NewGuid();
            
            var nonAdminUser = ParticipantFactory.GetNotAdminUser();

            var testParticipant = ParticipantFactory.GetParticipant(depositionId);

            var email = testParticipant.Email;

            var resourceActionList = new List<ResourceAction>()
            {
                It.IsAny<ResourceAction>()
            };

            ContextFactory.AddUserToContext(context.HttpContext, testParticipant.Email);
            _permissionsController.ControllerContext = context;

            _userService
                .Setup(mock => mock.GetUserByEmail(email))
                .ReturnsAsync(Result.Ok(nonAdminUser));
            
            _depositionService
                .Setup(mock => mock.GetDepositionParticipantByEmail(testParticipant.Id, testParticipant.Email))
                .ReturnsAsync(Result.Ok(testParticipant));

            _permissionService
                .Setup(mock => mock.GetDepositionUserPermissions(testParticipant, testParticipant.Id, false))
                .ReturnsAsync(Result.Ok(resourceActionList));

            // Act
            var result = await _permissionsController.GetDepositionPermissionsForParticipant(testParticipant.Id);
            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result.Result);
            _userService.Verify(mock => mock.GetUserByEmail(testParticipant.Email), Times.Once);
            _depositionService.Verify(mock => mock.GetDepositionParticipantByEmail(testParticipant.Id, testParticipant.Email), Times.Once);
            _permissionService.Verify(mock => mock.GetDepositionUserPermissions(testParticipant, testParticipant.Id, false), Times.Once);
        }

        [Fact]
        public async Task GetDepositionPermissionsForParticipant_Admin_ReturnsOk()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();

            var depositionId = Guid.NewGuid();

            var testParticipant = ParticipantFactory.GetParticipant(depositionId);

            var email = testParticipant.Email;

            var adminUser = ParticipantFactory.GetAdminUser();

            var resourceActionList = new List<ResourceAction>()
            {
                It.IsAny<ResourceAction>()
            };

            ContextFactory.AddUserToContext(context.HttpContext, testParticipant.Email);
            _permissionsController.ControllerContext = context;

            _userService
                .Setup(mock => mock.GetUserByEmail(email))
                .ReturnsAsync(Result.Ok(adminUser));

            _permissionService
                .Setup(mock => mock.GetDepositionUserPermissions(null, Guid.Empty, true))
                .ReturnsAsync(Result.Ok(resourceActionList));

            // Act
            var result = await _permissionsController.GetDepositionPermissionsForParticipant(testParticipant.Id);
            
            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result.Result);
            _userService.Verify(mock => mock.GetUserByEmail(testParticipant.Email), Times.Once);
            _permissionService.Verify(mock => mock.GetDepositionUserPermissions(null, Guid.Empty, true), Times.Once);
        }

        [Fact]
        public async Task GetDepositionPermissionsForParticipant_Admin_ReturnsError_WhenFails_DepositionService()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();

            var depositionId = Guid.NewGuid();

            var nonAdminUser = ParticipantFactory.GetNotAdminUser();

            var testParticipant = ParticipantFactory.GetParticipant(depositionId);

            var email = testParticipant.Email;

            var resourceActionList = new List<ResourceAction>()
            {
                It.IsAny<ResourceAction>()
            };

            ContextFactory.AddUserToContext(context.HttpContext, testParticipant.Email);
            _permissionsController.ControllerContext = context;

            _userService
                .Setup(mock => mock.GetUserByEmail(email))
                .ReturnsAsync(Result.Ok(nonAdminUser));

            _depositionService
                .Setup(mock => mock.GetDepositionParticipantByEmail(testParticipant.Id, testParticipant.Email))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _permissionsController.GetDepositionPermissionsForParticipant(testParticipant.Id);
            
            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetDepositionParticipantByEmail(testParticipant.Id, testParticipant.Email), Times.Once);
        }

        [Fact]
        public async Task GetDepositionPermissionsForParticipant_Admin_ReturnsError_WhenFails_PermissionService()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();

            var depositionId = Guid.NewGuid();

            var nonAdminUser = ParticipantFactory.GetNotAdminUser();

            var testParticipant = ParticipantFactory.GetParticipant(depositionId);

            var email = testParticipant.Email;

            var resourceActionList = new List<ResourceAction>()
            {
                It.IsAny<ResourceAction>()
            };

            ContextFactory.AddUserToContext(context.HttpContext, testParticipant.Email);
            _permissionsController.ControllerContext = context;

            _userService
                .Setup(mock => mock.GetUserByEmail(email))
                .ReturnsAsync(Result.Ok(nonAdminUser));

            _depositionService
                .Setup(mock => mock.GetDepositionParticipantByEmail(testParticipant.Id, testParticipant.Email))
                .ReturnsAsync(Result.Ok(testParticipant));

            _permissionService
                .Setup(mock => mock.GetDepositionUserPermissions(testParticipant, testParticipant.Id, false))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _permissionsController.GetDepositionPermissionsForParticipant(testParticipant.Id);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _permissionService.Verify(mock => mock.GetDepositionUserPermissions(testParticipant, testParticipant.Id, false), Times.Once);
        }
    }
}