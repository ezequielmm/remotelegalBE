using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class CompositionMapper : IMapper<Composition, CompositionDto, CallbackCompositionDto>
    {
        public CompositionDto ToDto(Composition model)
        {
            return new CompositionDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                Status = model.Status.ToString(),
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                LastUpdated = model.LastUpdated,
                SId = model.SId,
                Url = model.Url,
                MediaUrl = model.MediaUri,
                RoomId = model.RoomId
            };
        }

        public Composition ToModel(CompositionDto dto)
        {
            // TODO:  Replace this parse with a lib to handle Enums / strings parsing
            Enum.TryParse(dto.Status, true, out CompositionStatus compositionStatus);
            return new Composition
            {
                SId = dto.SId,
                Url = dto.Url,
                MediaUri = dto.MediaUrl,
                Status = compositionStatus
            };
        }

        public Composition ToModel(CallbackCompositionDto dto)
        {
            // TODO:  Replace this parse with a lib to handle Enums / strings parsing
            var status = dto.StatusCallbackEvent.Split("-")[1];
            Enum.TryParse(status, true, out CompositionStatus compositionStatus);

            return new Composition
            {
                SId = dto.CompositionSid,
                Url = dto.Url,
                MediaUri = dto.MediaUri,
                Status = compositionStatus,
                Room = new Room { SId = dto.RoomSid }
            };
        }
    }
}
