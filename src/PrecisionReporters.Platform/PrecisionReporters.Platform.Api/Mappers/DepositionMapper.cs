using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Linq;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class DepositionMapper : IMapper<Deposition, DepositionDto, CreateDepositionDto>
    {
        private readonly IMapper<Participant, ParticipantDto, CreateParticipantDto> _participantMapper;
        private readonly IMapper<Room, RoomDto, CreateRoomDto> _rooMapper;
        private readonly IMapper<Document, DocumentDto, CreateDocumentDto> _documentMapper;
        private readonly IMapper<DepositionDocument, DepositionDocumentDto, CreateDepositionDocumentDto> _depositionDocumentMapper;
        private readonly IMapper<User, UserDto, CreateUserDto> _userMapper;

        public DepositionMapper(IMapper<Participant, ParticipantDto, CreateParticipantDto> participantMapper, IMapper<Room, RoomDto,
            CreateRoomDto> rooMapper, IMapper<Document, DocumentDto, CreateDocumentDto> documentMapper,
            IMapper<User, UserDto, CreateUserDto> userMapper, IMapper<DepositionDocument, DepositionDocumentDto, CreateDepositionDocumentDto> depositionDocumentMapper)
        {
            _participantMapper = participantMapper;
            _rooMapper = rooMapper;
            _documentMapper = documentMapper;
            _userMapper = userMapper;
            _depositionDocumentMapper = depositionDocumentMapper;
        }

        public Deposition ToModel(DepositionDto dto)
        {
            return new Deposition
            {
                Id = dto.Id,
                CreationDate = dto.CreationDate.UtcDateTime,
                StartDate = dto.StartDate.UtcDateTime,
                TimeZone = dto.TimeZone,
                EndDate = dto.EndDate?.UtcDateTime,
                CompleteDate = dto.CompleteDate?.UtcDateTime,
                Witness = dto.Witness != null ? _participantMapper.ToModel(dto.Witness) : null,
                Participants = dto.Participants?.Select(p => _participantMapper.ToModel(p)).ToList(),
                RequesterId = dto.Requester.Id,
                Details = dto.Details,
                Room = _rooMapper.ToModel(dto.Room),
                Caption = dto.Caption != null ? _documentMapper.ToModel(dto.Caption) : null,
                Documents = dto.Documents?.Select(d => _depositionDocumentMapper.ToModel(d)).ToList()
            };
        }

        public Deposition ToModel(CreateDepositionDto dto)
        {
            return new Deposition
            {
                StartDate = dto.StartDate.UtcDateTime,
                TimeZone = dto.TimeZone,
                EndDate = dto.EndDate?.UtcDateTime,
                Witness = dto.Witness != null ? _participantMapper.ToModel(dto.Witness) : null,
                // TODO: Remove the creation of a new user and instead fulfill RequesterId property
                Requester = new User { EmailAddress = dto.RequesterEmail },
                Details = dto.Details,
                IsVideoRecordingNeeded = dto.IsVideoRecordingNeeded,
                FileKey = dto.Caption,
                Participants = dto.Participants?.Select(p => _participantMapper.ToModel(p)).ToList()
            };
        }

        public DepositionDto ToDto(Deposition model)
        {
            return new DepositionDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                StartDate = new DateTimeOffset(model.StartDate, TimeSpan.Zero),
                TimeZone = model.TimeZone,
                EndDate = model.EndDate.HasValue ? new DateTimeOffset(model.EndDate.Value, TimeSpan.Zero) : (DateTimeOffset?)null,
                Witness = model.Witness != null ? _participantMapper.ToDto(model.Witness) : null,
                Participants = model.Participants?.Select(p => _participantMapper.ToDto(p)).ToList(),
                Requester = model.Requester != null ? _userMapper.ToDto(model.Requester) : null,
                Details = model.Details,
                Room = model.Room != null ? _rooMapper.ToDto(model.Room) : null,
                Caption = model.Caption != null ? _documentMapper.ToDto(model.Caption) : null,
                Documents = model.Documents?.Select(d => _depositionDocumentMapper.ToDto(d)).ToList(),
                Status = model.Status,
                CaseId = model.CaseId,
                CaseName = model.Case?.Name,
                CaseNumber = model.Case?.CaseNumber,
                CompleteDate = model.CompleteDate,
                IsOnTheRecord = model.IsOnTheRecord
            };
        }
    }
}