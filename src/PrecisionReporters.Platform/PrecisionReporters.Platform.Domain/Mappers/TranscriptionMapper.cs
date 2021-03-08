using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class TranscriptionMapper : IMapper<Transcription, TranscriptionDto, object>
    {
        public Transcription ToModel(TranscriptionDto dto)
        {
            return new Transcription
            {
                Id = dto.Id,
                CreationDate = dto.CreationDate,
                Text = dto.Text,
                DepositionId = dto.DepositionId,
                UserId = dto.UserId,
                TranscriptDateTime = dto.TranscriptDateTime.UtcDateTime
            };
        }

        public TranscriptionDto ToDto(Transcription model)
        {
            return new TranscriptionDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                Text = model.Text,
                DepositionId = model.DepositionId,
                UserId = model.User.Id,
                UserName = $"{model.User.FirstName} {model.User.LastName}",
                TranscriptDateTime = new DateTimeOffset(model.TranscriptDateTime, TimeSpan.Zero),
                UserEmail = model.User  .EmailAddress
            };
        }        

        public Transcription ToModel(object dto)
        {
            throw new NotImplementedException();
        }
    }
}
