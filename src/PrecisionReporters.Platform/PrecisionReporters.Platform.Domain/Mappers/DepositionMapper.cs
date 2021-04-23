using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrecisionReporters.Platform.Domain.Mappers
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
            var witness = dto.Witness != null ? _participantMapper.ToModel(dto.Witness) : new Participant { Role = ParticipantType.Witness };
            return new Deposition
            {
                Id = dto.Id,
                CreationDate = dto.CreationDate.UtcDateTime,
                StartDate = dto.StartDate.UtcDateTime,
                TimeZone = !string.IsNullOrWhiteSpace(dto.TimeZone) ? ((USTimeZone)Enum.Parse(typeof(USTimeZone), dto.TimeZone)).GetDescription() : string.Empty,
                EndDate = dto.EndDate?.UtcDateTime,
                CompleteDate = dto.CompleteDate?.UtcDateTime,
                Participants = dto.Participants?.Select(p => _participantMapper.ToModel(p)).Append(witness).ToList(),
                RequesterId = dto.Requester != null ? dto.Requester.Id : Guid.Empty,
                Details = dto.Details,
                Room = dto.Room != null ? _rooMapper.ToModel(dto.Room) : null,
                Caption = dto.Caption != null ? _documentMapper.ToModel(dto.Caption) : null,
                Documents = dto.Documents?.Select(d => _depositionDocumentMapper.ToModel(d)).ToList(),
                Job = dto.Job,
                RequesterNotes = dto.RequesterNotes,
                Status = dto.Status,
                IsVideoRecordingNeeded = dto.IsVideoRecordingNeeded,
            };
        }

        public Deposition ToModel(CreateDepositionDto dto)
        {
            var witness = dto.Witness != null ? _participantMapper.ToModel(dto.Witness) : new Participant { Role = ParticipantType.Witness, IsAdmitted = true };
            return new Deposition
            {
                StartDate = dto.StartDate.UtcDateTime,
                TimeZone = ((USTimeZone)Enum.Parse(typeof(USTimeZone), dto.TimeZone)).GetDescription(),
                EndDate = dto.EndDate?.UtcDateTime,
                // TODO: Remove the creation of a new user and instead fulfill RequesterId property
                Requester = new User { EmailAddress = dto.RequesterEmail },
                Details = dto.Details,
                IsVideoRecordingNeeded = dto.IsVideoRecordingNeeded,
                FileKey = dto.Caption,
                Participants = dto.Participants != null
                    ? dto.Participants.Select(p => _participantMapper.ToModel(p)).Append(witness).ToList()
                    : new List<Participant> { witness }
            };
        }

        public DepositionDto ToDto(Deposition model)
        {
            var witness = model.Participants?.FirstOrDefault(x => x.Role == ParticipantType.Witness);
            var actualStartDate = model.GetActualStartDate();
            return new DepositionDto
            {
                Id = model.Id,
                CreationDate = new DateTimeOffset(model.CreationDate, TimeSpan.Zero),
                StartDate = new DateTimeOffset(model.StartDate, TimeSpan.Zero),
                TimeZone = Enum.GetValues(typeof(USTimeZone)).Cast<USTimeZone>().FirstOrDefault(x => x.GetDescription() == model.TimeZone).ToString(),
                EndDate = model.EndDate.HasValue ? new DateTimeOffset(model.EndDate.Value, TimeSpan.Zero) : (DateTimeOffset?)null,
                Witness = witness != null ? _participantMapper.ToDto(witness) : null,
                Participants = model.Participants?.Where(x => x.Role != ParticipantType.Witness).Select(p => _participantMapper.ToDto(p)).ToList(),
                Requester = model.Requester != null ? _userMapper.ToDto(model.Requester) : null,
                Details = model.Details,
                Room = model.Room != null ? _rooMapper.ToDto(model.Room) : null,
                Caption = model.Caption != null ? _documentMapper.ToDto(model.Caption) : null,
                Documents = model.Documents?.Select(d => _depositionDocumentMapper.ToDto(d)).ToList(),
                Status = model.Status,
                CaseId = model.CaseId,
                CaseName = model.Case?.Name,
                CaseNumber = model.Case?.CaseNumber,
                CompleteDate = model.CompleteDate.HasValue ? new DateTimeOffset(model.CompleteDate.Value, TimeSpan.Zero) : (DateTimeOffset?)null,
                IsOnTheRecord = model.IsOnTheRecord,
                SharingDocument = model.SharingDocument != null ? _documentMapper.ToDto(model.SharingDocument) : null,
                Job = model.Job,
                RequesterNotes = model.RequesterNotes,
                AddedBy = model.AddedBy != null ? _userMapper.ToDto(model.AddedBy) : null,
                EndedBy = model.EndedBy != null ? _userMapper.ToDto(model.EndedBy) : null,
                IsVideoRecordingNeeded = model.IsVideoRecordingNeeded,
                ActualStartDate = actualStartDate.HasValue ? new DateTimeOffset(actualStartDate.Value, TimeSpan.Zero) : (DateTimeOffset?)null
            };
        }
    }
}