using Microsoft.Extensions.Configuration;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class AnnotationEventRepositoryTest
    {
        private readonly DataAccessContextForTest _dataAccess;

        private AnnotationEventRepository _repository;

        private List<AnnotationEvent> annotations;


        private readonly Guid documentId1 = Guid.Parse("daf5f6f9-6ba8-40e5-8a5b-bd2303c556dc");
        private readonly Guid documentId2 = Guid.Parse("e3f109ce-b095-4744-9141-b7640f561eda");
        private readonly Guid documentId3 = Guid.Parse("981a96c7-731d-4ac9-acd4-16b030697054");

        private readonly Guid annotationId1 = Guid.Parse("1c147a9d-de27-4941-9505-56b4987d3787");
        private readonly Guid annotationId2 = Guid.Parse("d72d98f6-39d5-4d8c-a7af-1018393d539d");
        private readonly Guid annotationId3 = Guid.Parse("8fbddf26-2c74-48a7-9949-a757137bc6ab");

        public AnnotationEventRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _dataAccess.Database.EnsureDeleted();
            _dataAccess.Database.EnsureCreated();

            _repository = new AnnotationEventRepository(_dataAccess);
        }

        private async Task SeedDb()
        {
            annotations = new List<AnnotationEvent>
            {
                new AnnotationEvent {
                    DocumentId = documentId1,
                    Id = annotationId1,
                    Action = Platform.Data.Enums.AnnotationAction.Create,
                    Details = "test1",
                    Author = UserFactory.GetUserByGivenId(Guid.NewGuid()),
                    CreationDate = DateTime.UtcNow.AddDays(1)
                },
                new AnnotationEvent {
                    DocumentId = documentId2,
                    Id = annotationId2,
                    Action = Platform.Data.Enums.AnnotationAction.Create,
                    Details = "test2",
                    Author = UserFactory.GetUserByGivenId(Guid.NewGuid()),
                    CreationDate = DateTime.UtcNow.AddDays(2)

                },
                new AnnotationEvent {
                    DocumentId = documentId3,
                    Id = annotationId3,
                    Action = Platform.Data.Enums.AnnotationAction.Create,
                    Details = "test3",
                    Author = UserFactory.GetUserByGivenId(Guid.NewGuid()),
                    CreationDate = DateTime.UtcNow.AddDays(3)
                }
            };
            await _repository.CreateRange(annotations);
        }

        [Fact]
        public async Task GetAnnotationsByDocumentWithAnnotationId_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            // act
            var result = await _repository.GetAnnotationsByDocument(documentId3, annotationId1);

            // assert
            Assert.NotNull(result);
            Assert.Equal(result.ToString(), annotations.FindAll(x => x.DocumentId == documentId3).ToString());
            Assert.Equal(result.ToString(), annotations.FindAll(x => x.Details == "test3").ToString());
            Assert.Equal(result.ToString(), annotations.FindAll(x => x.Action == Platform.Data.Enums.AnnotationAction.Create).ToString());
            Assert.Equal(result.ToString(), annotations.FindAll(x => x.Author.FirstName == "FirstNameUser1").ToString());
            Assert.Equal(result.ToString(), annotations.FindAll(x => x.Author.LastName == "LastNameUser1").ToString());
        }
    }
}
