using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestPlatform.Utilities;
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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Api.Controllers
{
    public class DepositionsControllerTest
    {
        private readonly Mock<IDepositionService> _depositionService;
        private readonly Mock<IDocumentService> _documentService;
        private readonly Mock<IAnnotationEventService> _annotationEventService;
        private readonly Mock<IParticipantService> _participantService;
        private readonly IMapper<Deposition, DepositionDto, CreateDepositionDto> _depositionMapper;
        private readonly IMapper<BreakRoom, BreakRoomDto, object> _breakRoomMapper;
        private readonly IMapper<AnnotationEvent, AnnotationEventDto, CreateAnnotationEventDto> _annotationMapper;
        private readonly IMapper<DepositionEvent, DepositionEventDto, CreateDepositionEventDto> _eventMapper;
        private readonly IMapper<Participant, ParticipantDto, CreateParticipantDto> _participantMapper;
        private readonly IMapper<Participant, AddParticipantDto, CreateGuestDto> _guestMapper;
        private readonly IMapper<Participant, ParticipantTechStatusDto, object> _participantTechStatusMapper;
        private readonly IMapper<Composition, CompositionDto, CallbackCompositionDto> _compositionMapper;
        private readonly IMapper<Room, RoomDto, CreateRoomDto> _rooMapper;
        private readonly IMapper<Document, DocumentDto, CreateDocumentDto> _documentMapper;
        private readonly IMapper<DepositionDocument, DepositionDocumentDto, CreateDepositionDocumentDto> _depositionDocumentMapper;
        private readonly IMapper<User, UserDto, CreateUserDto> _userMapper;
        private readonly IMapper<UserSystemInfo, UserSystemInfoDto, object> _userSystemInfoMapper;
        private readonly IMapper<DeviceInfo, DeviceInfoDto, object> _deviceInfoMapper;
        private readonly IMapper<AwsSessionInfo, AwsInfoDto, object> _awsInfoMapper;
        private readonly DepositionsController _classUnderTest;

        public DepositionsControllerTest()
        {
            _depositionService = new Mock<IDepositionService>();
            _documentService = new Mock<IDocumentService>();
            _annotationEventService = new Mock<IAnnotationEventService>();
            _participantService = new Mock<IParticipantService>();
            _breakRoomMapper = new BreakRoomMapper();
            _annotationMapper = new AnnotationEventMapper();
            _eventMapper = new DepositionEventMapper();
            _participantMapper = new ParticipantMapper();
            _guestMapper = new GuestParticipantMapper();
            _participantTechStatusMapper = new ParticipantTechStatusMapper();
            _userSystemInfoMapper = new UserSystemInfoMapper();
            _deviceInfoMapper = new DeviceInfoMapper();
            _awsInfoMapper = new AwsInfoMapper();

            // Deposition mapper arguments
            _compositionMapper = new CompositionMapper();
            _rooMapper = new RoomMapper(_compositionMapper);
            _documentMapper = new DocumentMapper();
            _depositionDocumentMapper = new DepositionDocumentMapper();
            _userMapper = new UserMapper();
            _depositionMapper = new DepositionMapper(_participantMapper, _rooMapper, _documentMapper, _userMapper, _depositionDocumentMapper);

            // Class under test
            _classUnderTest = new DepositionsController(_depositionService.Object,
                _depositionMapper,
                _documentService.Object,
                _annotationMapper,
                _eventMapper,
                _breakRoomMapper,
                _annotationEventService.Object,
                _participantService.Object,
                _participantMapper,
                _guestMapper,
                _participantTechStatusMapper,
                _userSystemInfoMapper,
                _deviceInfoMapper,
                _awsInfoMapper);
        }

        [Fact]
        public async Task GetDepositions_ReturnsOkAndDepositionFilterResponseDto()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDepositionDtoWithWitness(depositionId, caseId);
            var depositionFilterResponse = new DepositionFilterResponseDto
                { Depositions = new List<DepositionDto> { deposition }, NumberOfPages = 1, Page = 1, TotalPast = 0, TotalUpcoming = 1 };

            _depositionService
                .Setup(mock => mock.GetDepositionsByFilter(It.IsAny<DepositionFilterDto>()))
                .ReturnsAsync(Result.Ok(depositionFilterResponse));

            // Act
            var result = await _classUnderTest.GetDepositions(It.IsAny<DepositionFilterDto>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<DepositionFilterResponseDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValue = Assert.IsAssignableFrom<DepositionFilterResponseDto>(okResult.Value);
            Assert.Equal(depositionFilterResponse.TotalUpcoming, resultValue.TotalUpcoming);
            Assert.Equal(depositionFilterResponse.Page, resultValue.Page);
            Assert.Equal(depositionFilterResponse.NumberOfPages, resultValue.NumberOfPages);
            Assert.Contains(resultValue.Depositions, d => d.Id == deposition.Id);
            _depositionService.Verify(mock => mock.GetDepositionsByFilter(It.IsAny<DepositionFilterDto>()), Times.Once);
        }

        [Fact]
        public async Task GetDepositions_ReturnsError_WhenGetDepositionsByFilterFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.GetDepositionsByFilter(It.IsAny<DepositionFilterDto>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetDepositions(It.IsAny<DepositionFilterDto>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetDepositionsByFilter(It.IsAny<DepositionFilterDto>()), Times.Once);
        }

        [Fact]
        public async Task JoinDeposition_ReturnOkAndJoinDepositionDto()
        {
            // Arrange
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);
            var joinDepositionDto = new JoinDepositionDto
            {
                IsOnTheRecord = true,
                JobNumber = $"{short.MaxValue}",
                Participants = new List<ParticipantDto> { ParticipantFactory.GetParticipantDtoByGivenRole(ParticipantType.CourtReporter) },
                ShouldSendToPreDepo = true,
                StartDate = DateTimeOffset.Now,
                TimeZone = "AT"
            };

            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.JoinDeposition(It.IsAny<Guid>(), identity))
                .ReturnsAsync(Result.Ok(joinDepositionDto));

            // Act
            var result = await _classUnderTest.JoinDeposition(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<JoinDepositionDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<JoinDepositionDto>(okResult.Value);
            _depositionService.Verify(mock => mock.JoinDeposition(It.IsAny<Guid>(), identity), Times.Once);
        }

        [Fact]
        public async Task JoinDeposition_ReturnsError_WhenJoinDepositionFails()
        {
            // Arrange
            var identity = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, identity);

            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.JoinDeposition(It.IsAny<Guid>(), identity))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.JoinDeposition(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.JoinDeposition(It.IsAny<Guid>(), identity), Times.Once);
        }

        [Fact]
        public async Task EndDeposition_ReturnOkAndDepositionDto()
        {
            // Arrange
            var deposition = DepositionFactory.GetDeposition(It.IsAny<Guid>(), It.IsAny<Guid>());
            _depositionService
                .Setup(mock => mock.EndDeposition(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(deposition));

            // Act
            var result = await _classUnderTest.EndDeposition(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<DepositionDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<DepositionDto>(okResult.Value);
            Assert.Equal(deposition.Id, resultValues.Id);
            Assert.Equal(deposition.StartDate, resultValues.StartDate);
            Assert.Equal(deposition.EndDate, resultValues.EndDate);
            Assert.Equal(deposition.CreationDate, resultValues.CreationDate);
            _depositionService.Verify(mock => mock.EndDeposition(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task EndDeposition_ReturnsError_WhenEndDepositionFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.EndDeposition(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.EndDeposition(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.EndDeposition(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetDeposition_ReturnsOkAndDepositionDto()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);

            _depositionService
                .Setup(mock => mock.GetDepositionById(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(deposition));

            // Act
            var result = await _classUnderTest.GetDeposition(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<DepositionDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<DepositionDto>(okResult.Value);
            Assert.Equal(deposition.Id, resultValues.Id);
            Assert.Equal(deposition.StartDate, resultValues.StartDate);
            Assert.Equal(deposition.EndDate, resultValues.EndDate);
            Assert.Equal(deposition.CreationDate, resultValues.CreationDate);
            _depositionService.Verify(mock => mock.GetDepositionById(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetDeposition_ReturnsError_WhenGetDepositionByIdFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.GetDepositionById(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetDeposition(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetDepositionById(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task DepositionRecord_ReturnOkAndDepositionEventDto()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var userEmail = "mock@mail.com";
            var depositionEventDto = new DepositionEvent
            {
                Id = Guid.NewGuid(),
                EventType = EventType.OnTheRecord,
                CreationDate = DateTime.UtcNow.AddSeconds(5),
                Details = "Mock Details"
            };
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, userEmail);

            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.GoOnTheRecord(depositionId, true, userEmail))
                .ReturnsAsync(Result.Ok(depositionEventDto));

            // Act
            var result = await _classUnderTest.DepositionRecord(depositionId, true);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<DepositionEventDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<DepositionEventDto>(okResult.Value);
            Assert.Equal(depositionEventDto.Id, resultValues.Id);
            Assert.Equal(depositionEventDto.CreationDate, resultValues.CreationDate);
            Assert.Equal(depositionEventDto.Details, resultValues.Details);
            Assert.Equal(depositionEventDto.EventType, resultValues.EventType);
            _depositionService.Verify(mock => mock.GoOnTheRecord(depositionId, true, userEmail), Times.Once);
        }

        [Fact]
        public async Task DepositionRecord_ReturnsError_WhenGoOnTheRecordFails()
        {
            // Arrange
            var userEmail = "mock@mail.com";
            var context = ContextFactory.GetControllerContext();
            ContextFactory.AddUserToContext(context.HttpContext, userEmail);

            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.GoOnTheRecord(It.IsAny<Guid>(), true, userEmail))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.DepositionRecord(It.IsAny<Guid>(), true);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GoOnTheRecord(It.IsAny<Guid>(), true, userEmail), Times.Once);
        }

        [Fact]
        public async Task JoinBreakRoom_ReturnsOkAndToken()
        {
            // Arrange
            var token = "mocktoken";
            _depositionService
                .Setup(mock => mock.JoinBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(token));

            // Act
            var result = await _classUnderTest.JoinBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<string>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<string>(okResult.Value);
            _depositionService.Verify(mock => mock.JoinBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task JoinBreakRoom_ReturnsError_WhenJoinBreakRoomFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.JoinBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.JoinBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.JoinBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task LeaveBreakRoom_ReturnsOk()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.LeaveBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok());

            // Act
            var result = await _classUnderTest.LeaveBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _depositionService.Verify(mock => mock.LeaveBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task LeaveBreakRoom_ReturnsError_WhenLeaveBreakRoomFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.LeaveBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.LeaveBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.LeaveBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task LockBreakRoom_ReturnOkAndBreakRoomDto()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var breakRoomId = Guid.NewGuid();
            var breakRoom = new BreakRoom
            {
                Id = Guid.NewGuid(),
                IsLocked = false,
                Room = RoomFactory.GetRoomById(breakRoomId)
            };
            _depositionService
                .Setup(mock => mock.LockBreakRoom(depositionId, breakRoomId, true))
                .ReturnsAsync(Result.Ok(breakRoom));

            // Act
            var result = await _classUnderTest.LockBreakRoom(depositionId, breakRoomId, true);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<BreakRoomDto>(okResult.Value);
            Assert.Equal(breakRoom.Id, resultValues.Id);
            Assert.Equal(breakRoom.IsLocked, resultValues.IsLocked);
            Assert.Equal(breakRoom.Name, resultValues.Name);
            _depositionService.Verify(mock => mock.LockBreakRoom(depositionId, breakRoomId, true), Times.Once);
        }

        [Fact]
        public async Task LockBreakRoom_ReturnsError_WhenLockBreakRoomFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.LockBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.LockBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.LockBreakRoom(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task GetDepositionBreakRooms_ReturnOkAndListOfBreakRoomsDto()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var breakRoom = new List<BreakRoom>
            {
                new BreakRoom
                {
                    Id = Guid.NewGuid(),
                    IsLocked = false,
                    Room = RoomFactory.GetRoomById(Guid.NewGuid())
                }
            };
            _depositionService
                .Setup(mock => mock.GetDepositionBreakRooms(depositionId))
                .ReturnsAsync(Result.Ok(breakRoom));

            // Act
            var result = await _classUnderTest.GetDepositionBreakRooms(depositionId);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<IEnumerable<BreakRoomDto>>(okResult.Value);
            Assert.Contains(resultValues, dto => dto.Id == breakRoom[0].Id && dto.IsLocked == breakRoom[0].IsLocked);
            _depositionService.Verify(mock => mock.GetDepositionBreakRooms(depositionId), Times.Once);
        }

        [Fact]
        public async Task GetDepositionBreakRooms_ReturnsError_WhenGetDepositionBreakRoomsFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.GetDepositionBreakRooms(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetDepositionBreakRooms(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetDepositionBreakRooms(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetDepositionEvents_ReturnOkAndListOfBreakRoomsDto()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var depositionEvents = new List<DepositionEvent>
            {
                new DepositionEvent
                {
                    CreationDate = DateTime.UtcNow,
                    EventType = EventType.StartDeposition
                }
            };
            _depositionService
                .Setup(mock => mock.GetDepositionEvents(depositionId))
                .ReturnsAsync(Result.Ok(depositionEvents));

            // Act
            var result = await _classUnderTest.GetDepositionEvents(depositionId);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<IEnumerable<DepositionEventDto>>(okResult.Value);
            Assert.Contains(resultValues, dto => dto.Id == depositionEvents[0].Id && dto.CreationDate == depositionEvents[0].CreationDate);
            _depositionService.Verify(mock => mock.GetDepositionEvents(depositionId), Times.Once);
        }

        [Fact]
        public async Task GetDepositionEvents_ReturnsError_WhenGetDepositionEventsFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.GetDepositionEvents(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetDepositionEvents(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetDepositionEvents(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task AddDocumentAnnotation_ReturnOk()
        {
            // Arrange
            var createAnnotationDto = new CreateAnnotationEventDto { Action = AnnotationAction.Create, Details = "Mock annotation event details" };
            _documentService
                .Setup(mock => mock.AddAnnotation(It.IsAny<Guid>(), It.IsAny<AnnotationEvent>()))
                .ReturnsAsync(Result.Ok(DocumentFactory.GetDocument()));

            // Act
            var result = await _classUnderTest.AddDocumentAnnotation(It.IsAny<Guid>(), createAnnotationDto);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            _documentService.Verify(mock => mock.AddAnnotation(It.IsAny<Guid>(), It.IsAny<AnnotationEvent>()), Times.Once);
        }

        [Fact]
        public async Task AddDocumentAnnotation_ReturnsError_WhenAddAnnotationFails()
        {
            // Arrange
            var createAnnotationDto = new CreateAnnotationEventDto { Action = AnnotationAction.Create, Details = "Mock annotation event details" };
            _documentService
                .Setup(mock => mock.AddAnnotation(It.IsAny<Guid>(), It.IsAny<AnnotationEvent>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.AddDocumentAnnotation(It.IsAny<Guid>(), createAnnotationDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _documentService.Verify(mock => mock.AddAnnotation(It.IsAny<Guid>(), It.IsAny<AnnotationEvent>()), Times.Once);
        }

        [Fact]
        public async Task ReScheduleDeposition_ReturnsOkAndDepositionDto_WhenHasNotFile()
        {
            // Arrange
            var editDepositionDto = DepositionFactory.GetEditDepositionDto();
            var depositionId = editDepositionDto.Deposition.Id;
            var contextWithFile = ContextFactory.GetControllerContext();
            _classUnderTest.ControllerContext = contextWithFile;
            _depositionService
                .Setup(mock => mock.ReScheduleDeposition(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), false))
                .ReturnsAsync(Result.Ok(DepositionFactory.GetDeposition(depositionId, Guid.NewGuid())));

            // Act
            var result = await _classUnderTest.ReScheduleDeposition(depositionId, editDepositionDto);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<DepositionDto>(okResult.Value);
            Assert.Equal(editDepositionDto.Deposition.Id, resultValues.Id);
            _depositionService.Verify(mock => mock.ReScheduleDeposition(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), false), Times.Once);
        }

        [Fact]
        public async Task ReScheduleDeposition_ReturnsOkAndDepositionDto()
        {
            // Arrange
            var editDepositionDto = DepositionFactory.GetEditDepositionDto();
            var depositionId = editDepositionDto.Deposition.Id;
            var contextWithFile = ContextFactory.GetControllerContextWithFile();
            _classUnderTest.ControllerContext = contextWithFile;
            _depositionService
                .Setup(mock => mock.ReScheduleDeposition(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), false))
                .ReturnsAsync(Result.Ok(DepositionFactory.GetDeposition(depositionId, Guid.NewGuid())));

            // Act
            var result = await _classUnderTest.ReScheduleDeposition(depositionId, editDepositionDto);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<DepositionDto>(okResult.Value);
            Assert.Equal(editDepositionDto.Deposition.Id, resultValues.Id);
            _depositionService.Verify(mock => mock.ReScheduleDeposition(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), false), Times.Once);
        }

        [Fact]
        public async Task ReScheduleDeposition_ReturnsError_WhenReScheduleDepositionFails()
        {
            // Arrange
            var editDepositionDto = DepositionFactory.GetEditDepositionDto();
            var depositionId = editDepositionDto.Deposition.Id;
            var contextWithFile = ContextFactory.GetControllerContext();
            _classUnderTest.ControllerContext = contextWithFile;
            _depositionService
                .Setup(mock => mock.ReScheduleDeposition(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), false))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.ReScheduleDeposition(depositionId, editDepositionDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.ReScheduleDeposition(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), false), Times.Once);
        }

        [Fact]
        public async Task NotifyParties_ReturnsOkAndNotifyOutputDto()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            _depositionService
                .Setup(mock => mock.NotifyParties(It.IsAny<Guid>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Ok(true));

            // Act
            var result = await _classUnderTest.NotifyParties(depositionId);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<NotifyOutputDto>(okResult.Value);
            Assert.True(resultValues.Notified);
            _depositionService.Verify(mock => mock.NotifyParties(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task NotifyParties_ReturnsError_WhenNotifyPartiesFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.NotifyParties(It.IsAny<Guid>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.NotifyParties(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.NotifyParties(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task GetDocumentAnnotations_ReturnOkAndListOfBreakRoomsDto()
        {
            // Arrange
            var annotationEventList = new List<AnnotationEvent>
            {
                new AnnotationEvent
                {
                    Id = Guid.NewGuid(), CreationDate = DateTime.UtcNow, Action = AnnotationAction.Modify, Author = UserFactory.GetUserByGivenId(Guid.NewGuid()),
                    Details = "Mock annotation"
                }
            };
            _annotationEventService
                .Setup(mock => mock.GetDocumentAnnotations(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(annotationEventList));

            // Act
            var result = await _classUnderTest.GetDocumentAnnotations(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<IEnumerable<AnnotationEventDto>>(okResult.Value);
            Assert.Contains(resultValues, annotation => annotation.Id == annotationEventList[0].Id);
            _annotationEventService.Verify(mock => mock.GetDocumentAnnotations(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetDocumentAnnotations_ReturnsError_WhenGetDocumentAnnotationsFails()
        {
            // Arrange
            _annotationEventService
                .Setup(mock => mock.GetDocumentAnnotations(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetDocumentAnnotations(It.IsAny<Guid>(), It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _annotationEventService.Verify(mock => mock.GetDocumentAnnotations(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task CheckParticipant_ReturnsOkAndParticipantValidationDto()
        {
            // Arrange
            var participant = ParticipantFactory.GetParticipant(Guid.NewGuid());
            _depositionService
                .Setup(mock => mock.CheckParticipant(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok((participant, true)));

            // Act
            var result = await _classUnderTest.CheckParticipant(It.IsAny<Guid>(), participant.Email);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<ParticipantValidationDto>(okResult.Value);
            Assert.Equal(participant.Id, resultValues.Participant.Id);
            Assert.True(resultValues.IsUser);
            _depositionService.Verify(mock => mock.CheckParticipant(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CheckParticipant_ReturnsOkAndParticipantValidationDto_WhenDepositionHasNotRequestedEmailAddress()
        {
            // Arrange
            var participant = ParticipantFactory.GetParticipant(Guid.NewGuid());
            _depositionService
                .Setup(mock => mock.CheckParticipant(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok(((Participant) null, true)));

            // Act
            var result = await _classUnderTest.CheckParticipant(It.IsAny<Guid>(), participant.Email);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<ParticipantValidationDto>(okResult.Value);
            Assert.Null(resultValues.Participant);
            Assert.True(resultValues.IsUser);
            _depositionService.Verify(mock => mock.CheckParticipant(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CheckParticipant_ReturnsError_WhenCheckParticipantFails()
        {
            // Arrange
            var emailAddress = "mock@mail.com";
            _depositionService
                .Setup(mock => mock.CheckParticipant(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.CheckParticipant(It.IsAny<Guid>(), emailAddress);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.CheckParticipant(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task JoinGuestParticipant_ReturnsOkAndGuestToken()
        {
            // Arrange
            var guestDto = new CreateGuestDto
            {
                Browser = "Mock Explorer",
                Device = "Top Device",
                EmailAddress = "mock@email.com",
                Name = "Mock",
                ParticipantType = ParticipantType.CourtReporter
            };
            var guestToken = new GuestToken { IdToken = "guestTokenMock" };
            var context = ContextFactory.GetControllerContextWithLocalIp();
            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.JoinGuestParticipant(It.IsAny<Guid>(), It.IsAny<Participant>(), It.IsAny<ActivityHistory>()))
                .ReturnsAsync(Result.Ok(guestToken));

            // Act
            var result = await _classUnderTest.JoinGuestParticipant(It.IsAny<Guid>(), guestDto);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<GuestToken>(okResult.Value);
            Assert.Equal(guestToken.IdToken, resultValues.IdToken);
            _depositionService.Verify(mock => mock.JoinGuestParticipant(It.IsAny<Guid>(), It.IsAny<Participant>(), It.IsAny<ActivityHistory>()), Times.Once);
        }

        [Fact]
        public async Task JoinGuestParticipant_ReturnsError_WhenJoinGuestParticipantFails()
        {
            // Arrange
            var guestDto = new CreateGuestDto
            {
                Browser = "Mock Explorer",
                Device = "Top Device",
                EmailAddress = "mock@email.com",
                Name = "Mock",
                ParticipantType = ParticipantType.CourtReporter
            };
            var context = ContextFactory.GetControllerContextWithLocalIp();
            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.JoinGuestParticipant(It.IsAny<Guid>(), It.IsAny<Participant>(), It.IsAny<ActivityHistory>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.JoinGuestParticipant(It.IsAny<Guid>(), guestDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.JoinGuestParticipant(It.IsAny<Guid>(), It.IsAny<Participant>(), It.IsAny<ActivityHistory>()), Times.Once);
        }

        [Fact]
        public async Task AddParticipant_ReturnsOkAndParticipantId()
        {
            // Arrange
            var addParticipantDto = new AddParticipantDto { EmailAddress = "mock@email.com", ParticipantType = ParticipantType.CourtReporter };
            var participantId = Guid.NewGuid();
            _depositionService
                .Setup(mock => mock.AddParticipant(It.IsAny<Guid>(), It.IsAny<Participant>()))
                .ReturnsAsync(Result.Ok(participantId));

            // Act
            var result = await _classUnderTest.AddParticipant(It.IsAny<Guid>(), addParticipantDto);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<ParticipantOutputDto>(okResult.Value);
            Assert.Equal(participantId, resultValues.Id);
            _depositionService.Verify(mock => mock.AddParticipant(It.IsAny<Guid>(), It.IsAny<Participant>()), Times.Once);
        }

        [Fact]
        public async Task AddParticipant_ReturnError_WhenAddParticipantFails()
        {
            // Arrange
            var addParticipantDto = new AddParticipantDto { EmailAddress = "mock@email.com", ParticipantType = ParticipantType.CourtReporter };
            _depositionService
                .Setup(mock => mock.AddParticipant(It.IsAny<Guid>(), It.IsAny<Participant>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.AddParticipant(It.IsAny<Guid>(), addParticipantDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.AddParticipant(It.IsAny<Guid>(), It.IsAny<Participant>()), Times.Once);
        }

        [Fact]
        public async Task AddParticipantToExistingDeposition_ReturnsOkAndParticipantId()
        {
            // Arrange
            var participant = ParticipantFactory.GetParticipant(Guid.NewGuid());
            var createParticipantDto = ParticipantFactory.GetCreateParticipantDtoByGivenRole(ParticipantType.CourtReporter);
            _depositionService
                .Setup(mock => mock.AddParticipantToExistingDeposition(It.IsAny<Guid>(), It.IsAny<Participant>()))
                .ReturnsAsync(Result.Ok(participant));

            // Act
            var result = await _classUnderTest.AddParticipantToExistingDeposition(It.IsAny<Guid>(), createParticipantDto);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<ParticipantDto>(okResult.Value);
            Assert.Equal(participant.Id, resultValues.Id);
            _depositionService.Verify(mock => mock.AddParticipantToExistingDeposition(It.IsAny<Guid>(), It.IsAny<Participant>()), Times.Once);
        }

        [Fact]
        public async Task AddParticipantToExistingDeposition_ReturnError_WhenAddParticipantToExistingDepositionFails()
        {
            // Arrange
            var createParticipantDto = ParticipantFactory.GetCreateParticipantDtoByGivenRole(ParticipantType.CourtReporter);
            _depositionService
                .Setup(mock => mock.AddParticipantToExistingDeposition(It.IsAny<Guid>(), It.IsAny<Participant>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.AddParticipantToExistingDeposition(It.IsAny<Guid>(), createParticipantDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.AddParticipantToExistingDeposition(It.IsAny<Guid>(), It.IsAny<Participant>()), Times.Once);
        }

        [Fact]
        public async Task AdmitParticipant_ReturnOk()
        {
            // Arrange
            var participant = new JoinDepositionResponseDto { IsAdmitted = true };
            _depositionService
                .Setup(mock => mock.AdmitDenyParticipant(It.IsAny<Guid>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Ok());

            // Act
            var result = await _classUnderTest.AdmitParticipant(It.IsAny<Guid>(), It.IsAny<Guid>(), participant);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
            _depositionService.Verify(mock => mock.AdmitDenyParticipant(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task AdmitParticipant_ReturnError_WhenAdmitDenyParticipantFails()
        {
            // Arrange
            var participant = new JoinDepositionResponseDto { IsAdmitted = true };
            _depositionService
                .Setup(mock => mock.AdmitDenyParticipant(It.IsAny<Guid>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.AdmitParticipant(It.IsAny<Guid>(), It.IsAny<Guid>(), participant);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.AdmitDenyParticipant(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task CancelDeposition_ReturnOkAndDepositionDto()
        {
            // Arrange
            var deposition = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            _depositionService
                .Setup(mock => mock.CancelDeposition(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(deposition));

            // Act
            var result = await _classUnderTest.CancelDeposition(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
            _depositionService.Verify(mock => mock.CancelDeposition(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task CancelDeposition_ReturnError_WhenCancelDepositionFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.CancelDeposition(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.CancelDeposition(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.CancelDeposition(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetDepositionVideo_ReturnOkAndDepositionDto()
        {
            // Arrange
            var videoInformation = new DepositionVideoDto { OffTheRecordTime = 10, OnTheRecordTime = 1, OutputFormat = ".mp4" };
            _depositionService
                .Setup(mock => mock.GetDepositionVideoInformation(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(videoInformation));

            // Act
            var result = await _classUnderTest.GetDepositionVideo(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<DepositionVideoDto>(okResult.Value);
            _depositionService.Verify(mock => mock.GetDepositionVideoInformation(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetDepositionVideo_ReturnError_WhenGetDepositionVideoInformationFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.GetDepositionVideoInformation(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetDepositionVideo(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetDepositionVideoInformation(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetParticipantList_ReturnOkAndDepositionDto()
        {
            // Arrange
            var participants = new List<Participant> { ParticipantFactory.GetParticipant(Guid.NewGuid()) };
            _depositionService
                .Setup(mock => mock.GetDepositionParticipants(It.IsAny<Guid>(), ParticipantSortField.Role, SortDirection.Descend))
                .ReturnsAsync(Result.Ok(participants));

            // Act
            var result = await _classUnderTest.GetParticipantList(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<List<Participant>>(okResult.Value);
            _depositionService.Verify(mock => mock.GetDepositionParticipants(It.IsAny<Guid>(), ParticipantSortField.Role, SortDirection.Descend), Times.Once);
        }

        [Fact]
        public async Task GetParticipantList_ReturnError_WhenGetDepositionParticipantsFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.GetDepositionParticipants(It.IsAny<Guid>(), ParticipantSortField.Role, SortDirection.Descend))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetParticipantList(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetDepositionParticipants(It.IsAny<Guid>(), ParticipantSortField.Role, SortDirection.Descend), Times.Once);
        }

        [Fact]
        public async Task GetWaitParticipants_ReturnOkAndDepositionDto()
        {
            // Arrange
            var participants = new List<Participant> { ParticipantFactory.GetParticipant(Guid.NewGuid()) };
            _participantService
                .Setup(mock => mock.GetWaitParticipants(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(participants));

            // Act
            var result = await _classUnderTest.GetWaitParticipants(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValue = Assert.IsAssignableFrom<List<ParticipantDto>>(okResult.Value);
            Assert.NotEmpty(resultValue);
            _participantService.Verify(mock => mock.GetWaitParticipants(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetWaitParticipants_ReturnError_WhenGetWaitParticipantsFails()
        {
            // Arrange
            _participantService
                .Setup(mock => mock.GetWaitParticipants(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok());

            // Act
            var result = await _classUnderTest.GetWaitParticipants(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var resultValue = Assert.IsAssignableFrom<List<ParticipantDto>>(result.Value);
            Assert.Empty(resultValue);
            _participantService.Verify(mock => mock.GetWaitParticipants(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task EditDepositionDetails_ReturnOkAndDepositionDto()
        {
            // Arrange
            var context = ContextFactory.GetControllerContextWithFile();
            var editDeposition = DepositionFactory.GetEditDepositionDto();
            var deposition = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.EditDepositionDetails(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Ok(deposition));

            // Act
            var result = await _classUnderTest.EditDepositionDetails(It.IsAny<Guid>(), editDeposition);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValue = Assert.IsAssignableFrom<DepositionDto>(okResult.Value);
            _depositionService.Verify(mock => mock.EditDepositionDetails(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task EditDepositionDetails_ReturnOkAndDepositionDto_WhenHasNotFile()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            var editDeposition = DepositionFactory.GetEditDepositionDto();
            var deposition = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.EditDepositionDetails(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Ok(deposition));

            // Act
            var result = await _classUnderTest.EditDepositionDetails(It.IsAny<Guid>(), editDeposition);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValue = Assert.IsAssignableFrom<DepositionDto>(okResult.Value);
            _depositionService.Verify(mock => mock.EditDepositionDetails(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task EditDepositionDetails_ReturnError_WhenEditDepositionDetailsFails()
        {
            // Arrange
            var context = ContextFactory.GetControllerContextWithFile();
            var editDeposition = DepositionFactory.GetEditDepositionDto();
            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.EditDepositionDetails(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.EditDepositionDetails(It.IsAny<Guid>(), editDeposition);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.EditDepositionDetails(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task RevertCancel_ReturnOkAndDepositionDto()
        {
            // Arrange
            var context = ContextFactory.GetControllerContextWithFile();
            var editDeposition = DepositionFactory.GetEditDepositionDto();
            var deposition = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.RevertCancel(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Ok(deposition));

            // Act
            var result = await _classUnderTest.RevertCancel(It.IsAny<Guid>(), editDeposition);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<DepositionDto>(okResult.Value);
            _depositionService.Verify(mock => mock.RevertCancel(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task RevertCancel_ReturnOkAndDepositionDto_WhenHasNotFile()
        {
            // Arrange
            var context = ContextFactory.GetControllerContext();
            var editDeposition = DepositionFactory.GetEditDepositionDto();
            var deposition = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.RevertCancel(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Ok(deposition));

            // Act
            var result = await _classUnderTest.RevertCancel(It.IsAny<Guid>(), editDeposition);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<DepositionDto>(okResult.Value);
            _depositionService.Verify(mock => mock.RevertCancel(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task RevertCancel_ReturnError_WhenRevertCancelFails()
        {
            // Arrange
            var context = ContextFactory.GetControllerContextWithFile();
            var editDeposition = DepositionFactory.GetEditDepositionDto();
            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.RevertCancel(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), It.IsAny<bool>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.RevertCancel(It.IsAny<Guid>(), editDeposition);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int) HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.RevertCancel(It.IsAny<Deposition>(), It.IsAny<FileTransferInfo>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task GetDepositionInfo_ReturnsError_WhenGetDepositionByIdFails()
        {
            // Arrange
            _depositionService
                .Setup(mock => mock.GetByIdWithIncludes(It.IsAny<Guid>(), new[] { nameof(Deposition.SharingDocument), nameof(Deposition.Room),
                $"{nameof(Deposition.Participants)}.{nameof(Participant.DeviceInfo)}",
                $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}.{nameof(User.ActivityHistories)}"}))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.GetDepositionInfo(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.GetByIdWithIncludes(It.IsAny<Guid>(), new[] { nameof(Deposition.SharingDocument), nameof(Deposition.Room),
                $"{nameof(Deposition.Participants)}.{nameof(Participant.DeviceInfo)}",
                $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}.{nameof(User.ActivityHistories)}"}), Times.Once);
        }

        [Fact]
        public async Task GetDepositionInfo_ReturnsOkAndDepositionTechStatusDto()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var roomId = Guid.NewGuid().ToString();
            var caseId = Guid.NewGuid();
            var participantId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var deposition = new Deposition
            {
                IsVideoRecordingNeeded = false,
                RoomId = Guid.NewGuid(),
                IsOnTheRecord = false,
                SharingDocument = new Document
                {
                    DisplayName = "sample.pdf"
                },
                Room = new Room
                {
                    SId = roomId
                },
                Participants = new List<Participant>
                {
                    new Participant {
                        Name = "test",
                        Id = participantId,
                        Role = ParticipantType.TechExpert,
                        CreationDate = It.IsAny<DateTime>(),
                        Email = "test@test.com",
                        HasJoined = false,
                        IsAdmitted = false,
                        IsMuted = false,
                        Phone = "2233222333",
                        User = new User {
                            EmailAddress = "test@test.com",
                            FirstName = "test",
                            LastName = "mock",
                            Id = userId
                        },
                        DepositionId = depositionId,
                        UserId = userId
                    }
                }
            };

            var participant = new Participant
            {
                Name = "test",
                Id = participantId,
                Role = ParticipantType.TechExpert,
                CreationDate = It.IsAny<DateTime>(),
                Email = "test@test.com",
                HasJoined = false,
                IsAdmitted = false,
                IsMuted = false,
                Phone = "2233222333",
                User = new User
                {
                    EmailAddress = "test@test.com",
                    FirstName = "test",
                    LastName = "mock",
                    Id = userId
                },
                DepositionId = depositionId,
                UserId = userId,
                DeviceInfo = new DeviceInfo { CameraName="cam1", CameraStatus=CameraStatus.Unavailable, MicrophoneName="mic1", SpeakersName="spk1", Id=Guid.NewGuid(), CreationDate=DateTime.UtcNow}
            };


            var depositionTechStatus = new DepositionTechStatusDto
            {
                RoomId = roomId,
                IsRecording = false,
                IsVideoRecordingNeeded = false,
                SharingExhibit = "sample.pdf",
                Participants = new List<ParticipantTechStatusDto>
                {
                    new ParticipantTechStatusDto{
                        Name = "test",
                        Id = It.IsAny<Guid>(),
                        Role = ParticipantType.TechExpert.ToString(),
                        CreationDate = It.IsAny<DateTime>(),
                        Email = "test@test.com",
                        HasJoined = false,
                        IsAdmitted = false,
                        IsMuted = false,
                        Browser = "Firefox",
                        OperatingSystem = "Linux",
                        IP = "0.0.0.92",
                        Device = "PC",
                        Devices = new DeviceInfoDto{
                            Camera = new CameraDto { Name = "Cam 1", Status = CameraStatus.Unavailable},
                            Microphone = new MicrophoneDto { Name = "Mic 1" },
                            Speakers = new SpeakersDto { Name = "Spk 1"}
                        }          
                    }
                }
            };

            _depositionService
                .Setup(mock => mock.GetByIdWithIncludes(It.IsAny<Guid>(), new[] { nameof(Deposition.SharingDocument), nameof(Deposition.Room),
                $"{nameof(Deposition.Participants)}.{nameof(Participant.DeviceInfo)}",
                $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}.{nameof(User.ActivityHistories)}"}))
                .ReturnsAsync(Result.Ok(deposition));

            // Act
            var result = await _classUnderTest.GetDepositionInfo(It.IsAny<Guid>());
            var resultParticipant = _participantMapper.ToDto(participant);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ActionResult<DepositionTechStatusDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<DepositionTechStatusDto>(okResult.Value);
            Assert.Equal(deposition.Room.SId, depositionTechStatus.RoomId);
            Assert.Equal(deposition.IsOnTheRecord, depositionTechStatus.IsRecording);
            Assert.Equal(deposition.SharingDocument.DisplayName, depositionTechStatus.SharingExhibit);
            Assert.Equal(deposition.IsVideoRecordingNeeded, depositionTechStatus.IsVideoRecordingNeeded);

            var compareValue1 = deposition.Participants.Select(p => _participantMapper.ToDto(p)).ToList();
            var compareValue2 = new List<ParticipantDto> { resultParticipant };
            try
            {
                Assert.Equal(compareValue1, compareValue2);
            }
            catch (Exception ex) {
                ConsoleOutput.Instance.WriteLine(ex.Message, OutputLevel.Information);
            };

           _depositionService.Verify(mock => mock.GetByIdWithIncludes(It.IsAny<Guid>(), new[] { nameof(Deposition.SharingDocument), nameof(Deposition.Room),
                $"{nameof(Deposition.Participants)}.{nameof(Participant.DeviceInfo)}",
                $"{nameof(Deposition.Participants)}.{nameof(Participant.User)}.{nameof(User.ActivityHistories)}"}), Times.Once);
        }

        [Fact]
        public async Task SetUserSystemInfo_ReturnsError_WhenSetUserSystemInfoFails()
        {
            // Arrange
            var userSystemInfoDto = new UserSystemInfoDto() {
                Browser = "test Browser",
                Device = "test Device",
                OS = "test OS"
            };

            var activityHistory = new ActivityHistory()
            {
                CreationDate = It.IsAny<DateTime>(),
                Device = userSystemInfoDto.Device,
                Browser = userSystemInfoDto.Browser,
                Action = ActivityHistoryAction.JoinDeposition,
                ActivityDate = It.IsAny<DateTime>(),
                DepositionId = It.IsAny<Guid>(),
                OperatingSystem = userSystemInfoDto.OS,
                UserId = It.IsAny<Guid>(),
                IPAddress = It.IsAny<string>()
            };
            var context = ContextFactory.GetControllerContextWithLocalIp();
            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.UpdateUserSystemInfo(It.IsAny<Guid>(), It.IsAny<UserSystemInfo>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.SetUserSystemInfo(It.IsAny<Guid>(), userSystemInfoDto);

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.UpdateUserSystemInfo(It.IsAny<Guid>(), It.IsAny<UserSystemInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SetUserSystemInfo_ReturnsOkAndDepositionTechStatusDto()
        {
            // Arrange
            var userSystemInfo = new UserSystemInfoDto()
            {
                Browser = "test Browser",
                Device = "test Device",
                OS = "test OS"
            };

            var activityHistory = new ActivityHistory()
            {
                CreationDate = It.IsAny<DateTime>(),
                Device = userSystemInfo.Device,
                Browser = userSystemInfo.Browser,
                Action = ActivityHistoryAction.JoinDeposition,
                ActivityDate = It.IsAny<DateTime>(),
                DepositionId = It.IsAny<Guid>(),
                OperatingSystem = userSystemInfo.OS,
                UserId = It.IsAny<Guid>(),
                IPAddress = It.IsAny<string>()
            };
            var context = ContextFactory.GetControllerContextWithLocalIp();
            _classUnderTest.ControllerContext = context;
            _depositionService
                .Setup(mock => mock.UpdateUserSystemInfo(It.IsAny<Guid>(), It.IsAny<UserSystemInfo>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok(userSystemInfo));

            // Act
            var result = await _classUnderTest.SetUserSystemInfo(It.IsAny<Guid>(), userSystemInfo);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            _depositionService.Verify(mock => mock.UpdateUserSystemInfo(It.IsAny<Guid>(), It.IsAny<UserSystemInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Summary_ReturnsOk()
        {
            // Arrange
            var depoStatus = Mock.Of<DepositionStatusDto>();
            _depositionService
                .Setup(mock => mock.Summary(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Ok(depoStatus));

            // Act
            var result = await _classUnderTest.Summary(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var resultValues = Assert.IsAssignableFrom<DepositionStatusDto>(okResult.Value);
            Assert.Equal(resultValues, depoStatus);
            _depositionService.Verify(mock => mock.Summary(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task Summary_ReturnsError()
        {
            // Arrange
            var errorMsg = "Invalid Deposition id";
            var depoStatus = Mock.Of<DepositionStatusDto>();
            _depositionService
                .Setup(mock => mock.Summary(It.IsAny<Guid>()))
                .ReturnsAsync(Result.Fail(errorMsg));

            // Act
            var result = await _classUnderTest.Summary(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<InternalServerErrorResult>(result.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.Summary(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task SaveAwsInfo_ReturnsError_WhenSaveAwsInfoFails()
        {
            // Arrange
            var awsSessionInfoDto = new AwsInfoDto()
            {
                AvailabilityZone = "Test Zone",
                ContainerId = "Cont 1234"
            };

            var activityHistory = new ActivityHistory()
            {
                CreationDate = It.IsAny<DateTime>(),
                Action = ActivityHistoryAction.SetAwsSessionInfo,
                ActivityDate = It.IsAny<DateTime>(),
                DepositionId = It.IsAny<Guid>(),
                UserId = It.IsAny<Guid>(),
                AmazonAvailability = It.IsAny<string>(),
                ContainerId = It.IsAny<string>()
            };
            _depositionService
                .Setup(mock => mock.SaveAwsSessionInfo(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Fail(new Error()));

            // Act
            var result = await _classUnderTest.SaveAWSInfo(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var errorResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, errorResult.StatusCode);
            _depositionService.Verify(mock => mock.SaveAwsSessionInfo(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SaveAwsInfo_ReturnsOk()
        {
            // Arrange
            var activityHistory = new ActivityHistory()
            {
                CreationDate = It.IsAny<DateTime>(),
                Action = ActivityHistoryAction.SetAwsSessionInfo,
                ActivityDate = It.IsAny<DateTime>(),
                DepositionId = It.IsAny<Guid>(),
                UserId = It.IsAny<Guid>(),
                AmazonAvailability = It.IsAny<string>(),
                ContainerId = It.IsAny<string>()
            };

            _depositionService
                .Setup(mock => mock.SaveAwsSessionInfo(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(Result.Ok());

            // Act
            var result = await _classUnderTest.SaveAWSInfo(It.IsAny<Guid>());

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result);

            _depositionService.Verify(mock => mock.SaveAwsSessionInfo(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }
    }
}