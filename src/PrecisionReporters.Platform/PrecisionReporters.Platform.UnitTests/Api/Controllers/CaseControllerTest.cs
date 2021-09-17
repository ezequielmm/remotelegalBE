using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PrecisionReporters.Platform.Api.Controllers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Helpers;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Api.Controllers
{
    public class CaseControllerTest
    {
        private readonly Mock<ICaseService> _caseServiceMock;
        private readonly Mock<IMapper<Case, CaseDto, CreateCaseDto>> _caseMapperMock;
        private readonly Mock<IMapper<Deposition, DepositionDto, CreateDepositionDto>> _depositionMapperMock;
        private readonly Mock<IMapper<Case, EditCaseDto, object>> _editCaseMapperMock;

        private readonly CasesController _caseController;

        public CaseControllerTest()
        {
            _caseServiceMock = new Mock<ICaseService>();
            _caseMapperMock = new Mock<IMapper<Case, CaseDto, CreateCaseDto>>();
            _depositionMapperMock = new Mock<IMapper<Deposition, DepositionDto, CreateDepositionDto>>();
            _editCaseMapperMock = new Mock<IMapper<Case, EditCaseDto, object>>();
            _caseController = new CasesController(_caseServiceMock.Object, _caseMapperMock.Object, _depositionMapperMock.Object, _editCaseMapperMock.Object);
        }


        [Fact]
        public async Task CreateCase_ShouldFail_CreateCaseService()
        {
            //Arrange
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);
            _caseController.ControllerContext = context;
            _caseServiceMock.Setup(c => c.CreateCase(It.IsAny<string>(), It.IsAny<Case>())).ReturnsAsync(Result.Fail(new Error()));

            //Act
            var result = await _caseController.CreateCase(It.IsAny<CreateCaseDto>());

            //Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _caseServiceMock.Verify(mock => mock.CreateCase(It.IsAny<string>(), It.IsAny<Case>()), Times.Once);
        }

        [Fact]
        public async Task CreateCase_ShouldOk()
        {
            //Arrange
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);
            _caseController.ControllerContext = context;
            _caseServiceMock.Setup(c => c.CreateCase(It.IsAny<string>(), It.IsAny<Case>())).ReturnsAsync(Result.Ok(new Case()));

            //Act
            var result = await _caseController.CreateCase(It.IsAny<CreateCaseDto>());

            //Assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result.Result);
            _caseServiceMock.Verify(mock => mock.CreateCase(It.IsAny<string>(), It.IsAny<Case>()), Times.Once);
        }

        [Fact]
        public async Task GetCaseById_ShouldFail_GetByIdService()
        {
            //Arrange
            _caseServiceMock.Setup(c => c.GetCaseById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(Result.Fail(new Error()));

            //Act
            var result = await _caseController.GetCaseById(It.IsAny<Guid>());

            //Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _caseServiceMock.Verify(mock => mock.GetCaseById(It.IsAny<Guid>(), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task GetCaseById_ShouldOk()
        {
            //Arrange
            _caseServiceMock.Setup(c => c.GetCaseById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync(Result.Ok(new Case()));

            //Act
            var result = await _caseController.GetCaseById(It.IsAny<Guid>());

            //Assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result.Result);
            _caseServiceMock.Verify(mock => mock.GetCaseById(It.IsAny<Guid>(), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task GetCasesForCurrentUser_ShouldFail_GetForUserService()
        {
            //Arrange
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);
            _caseController.ControllerContext = context;
            _caseServiceMock.Setup(c => c.GetCasesForUser(It.IsAny<string>(), It.IsAny<CaseSortField>(), It.IsAny<SortDirection>())).ReturnsAsync(Result.Fail(new Error()));

            //Act
            var result = await _caseController.GetCasesForCurrentUser(It.IsAny<CaseSortField>(), It.IsAny<SortDirection>());

            //Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _caseServiceMock.Verify(mock => mock.GetCasesForUser(It.IsAny<string>(), It.IsAny<CaseSortField>(), It.IsAny<SortDirection>()), Times.Once);
        }

        [Fact]
        public async Task GetCasesForCurrentUser_ShouldOk()
        {
            //Arrange
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);
            _caseController.ControllerContext = context;
            _caseServiceMock.Setup(c => c.GetCasesForUser(It.IsAny<string>(), It.IsAny<CaseSortField>(), It.IsAny<SortDirection>())).ReturnsAsync(Result.Ok(new List<Case>()));

            //Act
            var result = await _caseController.GetCasesForCurrentUser(It.IsAny<CaseSortField>(), It.IsAny<SortDirection>());

            //Assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result.Result);
            _caseServiceMock.Verify(mock => mock.GetCasesForUser(It.IsAny<string>(), It.IsAny<CaseSortField>(), It.IsAny<SortDirection>()), Times.Once);
        }

        [Fact]
        public async Task EditCase_ShouldFail_EditCaseService()
        {
            //Arrange
            _caseServiceMock.Setup(c => c.EditCase(It.IsAny<Case>())).ReturnsAsync(Result.Fail(new Error()));
            _editCaseMapperMock.Setup(m => m.ToModel(It.IsAny<EditCaseDto>())).Returns(new Case { Id = Guid.NewGuid() });
            //Act
            var result = await _caseController.EditCase(It.IsAny<Guid>(), It.IsAny<EditCaseDto>());

            //Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _caseServiceMock.Verify(mock => mock.EditCase(It.IsAny<Case>()), Times.Once);
        }

        [Fact]
        public async Task EditCase_ShouldOk()
        {
            //Arrange            
            _caseServiceMock.Setup(c => c.EditCase(It.IsAny<Case>())).ReturnsAsync(Result.Ok(new Case()));
            _editCaseMapperMock.Setup(m => m.ToModel(It.IsAny<EditCaseDto>())).Returns(new Case { Id = Guid.NewGuid() });
            //Act
            var result = await _caseController.EditCase(It.IsAny<Guid>(), It.IsAny<EditCaseDto>());

            //Assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result.Result);
            _caseServiceMock.Verify(mock => mock.EditCase(It.IsAny<Case>()), Times.Once);
        }

        [Fact]
        public async Task ScheduleDeposition_ShouldFail_NullDeposition()
        {
            //Arrange
            var casePatchMock = new CasePatchDto();

            //Act
            var result = await _caseController.ScheduleDepositions(It.IsAny<Guid>(), casePatchMock);

            //Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.BadRequest, errorResult.StatusCode);
        }

        [Fact]
        public async Task ScheduleDeposition_ShouldFail_ScheduleDepositionService()
        {
            //Arrange
            var context = ContextFactory.GetControllerContextWithFile();
            _caseController.ControllerContext = context;
            var createDepositionMock = new CreateDepositionDto
            {
                StartDate = DateTime.Now
            };
            var casePatchMock = new CasePatchDto
            {
                Depositions = new List<CreateDepositionDto>()
                {
                    createDepositionMock
                }
            };

            _depositionMapperMock.Setup(m => m.ToModel(It.IsAny<CreateDepositionDto>())).Returns(new Deposition());

            _caseServiceMock.Setup(
                c => c.ScheduleDepositions(It.IsAny<Guid>(), It.IsAny<IEnumerable<Deposition>>(), It.IsAny<Dictionary<string, FileTransferInfo>>()))
                .ReturnsAsync(Result.Fail(new Error()));
            //Act
            var result = await _caseController.ScheduleDepositions(It.IsAny<Guid>(), casePatchMock);

            //Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
        }

        [Fact]
        public async Task ScheduleDeposition_ShouldOk()
        {
            //Arrange
            var context = ContextFactory.GetControllerContextWithFile();
            _caseController.ControllerContext = context;
            var createDepositionMock = new CreateDepositionDto
            {
                StartDate = DateTime.Now
            };
            var casePatchMock = new CasePatchDto
            {
                Depositions = new List<CreateDepositionDto>()
                {
                    createDepositionMock
                }
            };

            _depositionMapperMock.Setup(m => m.ToModel(It.IsAny<CreateDepositionDto>())).Returns(new Deposition());
            _depositionMapperMock.Setup(m => m.ToDto(It.IsAny<Deposition>())).Returns(new DepositionDto());
            _caseServiceMock.Setup(
                c => c.ScheduleDepositions(It.IsAny<Guid>(), It.IsAny<IEnumerable<Deposition>>(), It.IsAny<Dictionary<string, FileTransferInfo>>()))
                .ReturnsAsync(Result.Ok(new Case { Depositions = new List<Deposition>() }));
            //Act
            var result = await _caseController.ScheduleDepositions(It.IsAny<Guid>(), casePatchMock);

            //Assert
            Assert.NotNull(result);
            Assert.IsType<ObjectResult>(result.Result);
            _caseServiceMock.Verify(mock => mock.ScheduleDepositions(It.IsAny<Guid>(), It.IsAny<IEnumerable<Deposition>>(), It.IsAny<Dictionary<string, FileTransferInfo>>()), Times.Once);
        }
    }
}
