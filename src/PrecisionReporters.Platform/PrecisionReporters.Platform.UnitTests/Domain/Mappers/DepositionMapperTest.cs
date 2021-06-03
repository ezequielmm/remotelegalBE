using System;
using System.Collections.Generic;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Enums;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.UnitTests.Utils;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class DepositionMapperTest
    {
        private readonly IMock<IMapper<Room, RoomDto, CreateRoomDto>> _rooMapper;
        private readonly IMock<IMapper<Document, DocumentDto, CreateDocumentDto>> _documentMapper;
        private readonly DepositionDocumentMapper _depositionDocumentMapper;
        private readonly ParticipantMapper _participantMapper;
        private readonly UserMapper _userMapper;
        private readonly DepositionMapper _classUnderTest;

        public DepositionMapperTest()
        {
            _rooMapper = new Mock<IMapper<Room, RoomDto, CreateRoomDto>>();
            _documentMapper = new Mock<IMapper<Document, DocumentDto, CreateDocumentDto>>();
            _depositionDocumentMapper = new DepositionDocumentMapper();
            _participantMapper = new ParticipantMapper();
            _userMapper = new UserMapper();

            _classUnderTest = new DepositionMapper(_participantMapper,
                _rooMapper.Object,
                _documentMapper.Object,
                _userMapper,
                _depositionDocumentMapper);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithDepositionDtoAndWitness()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var witnessDto = ParticipantFactory.GetParticipantDtoByGivenRole(ParticipantType.Witness);
            var courtReporterDto = ParticipantFactory.GetParticipantDtoByGivenRole(ParticipantType.CourtReporter);
            var attorneyDto = ParticipantFactory.GetParticipantDtoByGivenRole(ParticipantType.Attorney);
            var user = UserFactory.GetCreateUserDto();
            var depositionDocument = new DepositionDocumentDto
            {
                Id = Guid.NewGuid(),
                CreationDate = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero),
                DepositionId = depositionId,
                DocumentId = Guid.NewGuid()
            };

            var dto = new DepositionDto
            {
                Id = depositionId,
                CreationDate = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero),
                StartDate = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero),
                TimeZone = USTimeZone.ET.ToString(),
                Witness = witnessDto,
                Participants = new List<ParticipantDto> { courtReporterDto, attorneyDto },
                Requester = user,
                Details = "Details of a mock deposition",
                Documents = new List<DepositionDocumentDto> { depositionDocument },
                Status = DepositionStatus.Pending,
                CaseId = Guid.NewGuid(),
                IsOnTheRecord = false,
                Job = "33000",
                RequesterNotes = "",
                AddedBy = user,
                IsVideoRecordingNeeded = false
            };

            // Act
            var result = _classUnderTest.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result.Participants, p => p.Id == witnessDto.Id);
            Assert.Contains(result.Documents, document => document.Id == depositionDocument.Id);

            Assert.Equal(dto.Id, result.Id);
            Assert.Equal(dto.CreationDate.ToLocalTime(), result.CreationDate);
            Assert.Equal(dto.StartDate, result.StartDate);
            Assert.Equal(dto.CreationDate, result.CreationDate);
            Assert.Equal(USTimeZone.ET.GetDescription(), result.TimeZone);
            Assert.Equal(dto.EndDate, result.EndDate);
            Assert.Equal(dto.Requester.Id, result.RequesterId);
            Assert.Equal(dto.Details, result.Details);
            Assert.Equal(dto.Status, result.Status);
            Assert.Equal(dto.Job, result.Job);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithDepositionDtoAndWithoutWitness()
        {
            // Arrange
            var courtReporterDto = ParticipantFactory.GetParticipantDtoByGivenRole(ParticipantType.CourtReporter);
            var user = UserFactory.GetCreateUserDto();

            var dto = new DepositionDto
            {
                Id = Guid.NewGuid(),
                CreationDate = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero),
                StartDate = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero),
                TimeZone = USTimeZone.ET.ToString(),
                Participants = new List<ParticipantDto> { courtReporterDto },
                Requester = user,
                Details = "Details of a mock deposition",
                Documents = new List<DepositionDocumentDto>(),
                Status = DepositionStatus.Pending,
                CaseId = Guid.NewGuid(),
                IsOnTheRecord = false,
                Job = "33000",
                RequesterNotes = "",
                AddedBy = user,
                IsVideoRecordingNeeded = false
            };

            // Act
            var result = _classUnderTest.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result.Participants, p => p.Role == ParticipantType.Witness);
            Assert.Equal(dto.Id, result.Id);
            Assert.Equal(dto.StartDate, result.StartDate);
            Assert.Equal(dto.CreationDate, result.CreationDate);
            Assert.Equal(USTimeZone.ET.GetDescription(), result.TimeZone);
            Assert.Equal(dto.EndDate, result.EndDate);
            Assert.Equal(dto.Requester.Id, result.RequesterId);
            Assert.Equal(dto.Details, result.Details);
            Assert.Equal(dto.Status, result.Status);
            Assert.Equal(dto.Job, result.Job);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCreateDepositionDtoAndWitness()
        {
            // Arrange
            var witnessDto = ParticipantFactory.GetCreateParticipantDtoByGivenRole(ParticipantType.Witness);
            var courtReporterDto = ParticipantFactory.GetCreateParticipantDtoByGivenRole(ParticipantType.CourtReporter);
            var attorneyDto = ParticipantFactory.GetCreateParticipantDtoByGivenRole(ParticipantType.Attorney);
            var requesterEmail = "mock@mail.com";

            var createDepositionDto = new CreateDepositionDto
            {
                StartDate = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero),
                EndDate = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero).AddDays(5),
                TimeZone = USTimeZone.ET.ToString(),
                Caption = "",
                Witness = witnessDto,
                IsVideoRecordingNeeded = false,
                RequesterEmail = requesterEmail,
                Details = "Details of a mock deposition",
                Participants = new List<CreateParticipantDto> { courtReporterDto, attorneyDto }
            };

            // Act
            var result = _classUnderTest.ToModel(createDepositionDto);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result.Participants, p => p.Role == ParticipantType.Witness && p.Email.Equals(witnessDto.Email.ToLower()));
            Assert.Equal(createDepositionDto.StartDate, result.StartDate);
            Assert.Equal(USTimeZone.ET.GetDescription(), result.TimeZone);
            Assert.Equal(createDepositionDto.EndDate, result.EndDate);
            Assert.Equal(createDepositionDto.Details, result.Details);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCreateDepositionDtoAndWithoutWitness()
        {
            // Arrange
            var courtReporterDto = ParticipantFactory.GetCreateParticipantDtoByGivenRole(ParticipantType.CourtReporter);
            var attorneyDto = ParticipantFactory.GetCreateParticipantDtoByGivenRole(ParticipantType.Attorney);
            var requesterEmail = "mock@mail.com";

            var createDepositionDto = new CreateDepositionDto
            {
                StartDate = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero),
                EndDate = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero).AddDays(5),
                TimeZone = USTimeZone.ET.ToString(),
                Caption = "",
                IsVideoRecordingNeeded = false,
                RequesterEmail = requesterEmail,
                Details = "Details of a mock deposition",
                Participants = new List<CreateParticipantDto> { courtReporterDto, attorneyDto }
            };

            // Act
            var result = _classUnderTest.ToModel(createDepositionDto);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result.Participants, p => p.Role == ParticipantType.Witness);
            Assert.Equal(createDepositionDto.StartDate, result.StartDate);
            Assert.Equal(USTimeZone.ET.GetDescription(), result.TimeZone);
            Assert.Equal(createDepositionDto.EndDate, result.EndDate);
            Assert.Equal(createDepositionDto.Details, result.Details);
        }

        [Fact]
        public void ToDto_ShouldReturnDto()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var witness = _participantMapper.ToModel(ParticipantFactory.GetParticipantDtoByGivenRole(ParticipantType.Witness));
            var courtReporter = _participantMapper.ToModel(ParticipantFactory.GetParticipantDtoByGivenRole(ParticipantType.CourtReporter));
            var attorney = _participantMapper.ToModel(ParticipantFactory.GetParticipantDtoByGivenRole(ParticipantType.Attorney));
            var user = _userMapper.ToModel(UserFactory.GetCreateUserDto());

            var model = new Deposition
            {
                Id = depositionId,
                CreationDate = DateTime.UtcNow,
                StartDate = DateTime.UtcNow.AddDays(1),
                TimeZone = USTimeZone.ET.GetDescription(),
                Participants = new List<Participant> { courtReporter, attorney, witness },
                Requester = user,
                Details = "Details of a mock deposition",
                Status = DepositionStatus.Pending,
                CaseId = Guid.NewGuid(),
                IsOnTheRecord = false,
                Job = "33000",
                RequesterNotes = "",
                AddedBy = user,
                IsVideoRecordingNeeded = false
            };

            var depositionDocument = new DepositionDocument
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.Now,
                Document = new Document { Id = Guid.NewGuid() },
                Deposition = model
            };
            model.Documents = new List<DepositionDocument> { depositionDocument };

            // Act
            var result = _classUnderTest.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain(result.Participants, p => p.Role == ParticipantType.Witness.ToString() && p.Id == witness.UserId);
            Assert.Contains(result.Documents, document => document.Id == depositionDocument.Id);
            Assert.Equal(witness.Id, result.Witness.Id);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.StartDate, result.StartDate);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(USTimeZone.ET.ToString(), result.TimeZone);
            Assert.Equal(model.Details, result.Details);
            Assert.Equal(model.Status, result.Status);
            Assert.Equal(model.Job, result.Job);
            Assert.Equal(model.AddedBy.Id, result.AddedBy.Id);
            Assert.Equal(model.Requester.Id, result.Requester.Id);
        }
    }
}