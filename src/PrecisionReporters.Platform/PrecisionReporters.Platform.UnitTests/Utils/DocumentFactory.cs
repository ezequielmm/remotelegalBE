using System;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    public static class DocumentFactory
    {
        public static DocumentDto GetDocumentDtoByDocumentType(DocumentType documentType)
        {
            return new DocumentDto
            {
                Id = Guid.NewGuid(),
                CreationDate = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero),
                DisplayName = "Mock Display Name",
                Size = 1088,
                Name = "Mock Document",
                AddedBy = new UserOutputDto
                {
                    Id = Guid.NewGuid(),
                },
                SharedAt = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero),
                DocumentType = documentType
            };
        }

        public static CreateDocumentDto GetCreateDocumentDtoByDocumentType()
        {
            return new CreateDocumentDto()
            {
                Name = "Mock Create Document"
            };
        }

        public static Document GetDocument()
        {
            return new Document
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                DisplayName = "Mock Display Name",
                Size = 1088,
                Name = "Mock Document",
                AddedBy = new User {
                    Id = Guid.NewGuid(),
                    FirstName = "Name",
                    LastName = "Lastname"
                },
                SharedAt = DateTime.UtcNow
            };
        }
    }
}