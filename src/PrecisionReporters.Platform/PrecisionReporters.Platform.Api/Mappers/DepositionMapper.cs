using System.Linq;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class DepositionMapper : IMapper<Deposition, DepositionDto, CreateDepositionDto>
    {
        private readonly IMapper<Participant, ParticipantDto, CreateParticipantDto> _participantMapper;
        private readonly IMapper<Room, RoomDto, CreateRoomDto> _rooMapper;
        private readonly IMapper<DepositionDocument, DepositionDocumentDto, CreateDepositionDocumentDto> _depositionDocumentMapper;
        private readonly IMapper<Case, CaseDto, CreateCaseDto> _caseMapper;
        private readonly IMapper<User, UserDto, CreateUserDto> _userMapper;

        public DepositionMapper(IMapper<Participant, ParticipantDto, CreateParticipantDto> participantMapper, IMapper<Room, RoomDto,
            CreateRoomDto> rooMapper, IMapper<DepositionDocument, DepositionDocumentDto, CreateDepositionDocumentDto> depositionDocumentMapper,
            IMapper<Case, CaseDto, CreateCaseDto> caseMapper, IMapper<User, UserDto, CreateUserDto> userMapper)
        {
            _participantMapper = participantMapper;
            _rooMapper = rooMapper;
            _depositionDocumentMapper = depositionDocumentMapper;
            _caseMapper = caseMapper;
            _userMapper = userMapper;
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
                Witness = dto.Witness != null ? _participantMapper.ToModel(dto.Witness) : null,
                Participants = dto.Participants?.Select(p => _participantMapper.ToModel(p)).ToList(),
                RequesterId = dto.Requester.Id,
                Details = dto.Details,
                Room = _rooMapper.ToModel(dto.Room),
                Caption = dto.Caption != null ? _depositionDocumentMapper.ToModel(dto.Caption) : null,
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
                IsVideoRecordingNeeded = dto.IsVideoRecordingNeeded ,
                FileKey = dto.Caption,
                Participants = dto.Participants?.Select(p=>_participantMapper.ToModel(p)).ToList()
            };
        }

        public DepositionDto ToDto(Deposition model)
        {
            return new DepositionDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                StartDate = model.StartDate,
                TimeZone = model.TimeZone,
                EndDate = model.EndDate,
                Witness = model.Witness != null ? _participantMapper.ToDto(model.Witness) : null,
                Participants = model.Participants?.Select(p => _participantMapper.ToDto(p)).ToList(),
                Requester = _userMapper.ToDto(model.Requester),
                Details = model.Details,
                Room = model.Room != null ? _rooMapper.ToDto(model.Room) : null,
                Caption = model.Caption != null ? _depositionDocumentMapper.ToDto(model.Caption) : null,
                Documents = model.Documents?.Select(d => _depositionDocumentMapper.ToDto(d)).ToList(),
                Status = model.Status,
                CaseName = model.Case.Name,
                CaseNumber = model.Case.CaseNumber
            };
        }
    }
}