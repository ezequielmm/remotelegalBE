using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;
using System;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class DocumentWithSignedUrlMapper : IMapper<Document, DocumentWithSignedUrlDto, object>
    {
        public DocumentWithSignedUrlDto ToDto(Document model)
        {
            return new DocumentWithSignedUrlDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                DisplayName = model.DisplayName,
                Size = model.Size,
                Name = model.Name,
                AddedBy = model.AddedBy != null ? new UserOutputDto
                {
                    Id = model.AddedBy.Id,
                    FirstName = model.AddedBy.FirstName,
                    LastName = model.AddedBy.LastName
                } : null
            };
        }

        public Document ToModel(DocumentWithSignedUrlDto dto)
        {
            throw new NotImplementedException();
        }

        public Document ToModel(object dto)
        {
            throw new NotImplementedException();
        }
    }
}
