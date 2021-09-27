using Microsoft.Extensions.Configuration;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Repositories
{
    public class AnnotationEventRepositoryTest
    {
        private readonly Mock<IConfiguration> _configuration;
        private readonly DataAccessContextForTest _dataAccess;

        private AnnotationEventRepository _repository;

        private readonly Guid documentId1 = Guid.Parse("daf5f6f9-6ba8-40e5-8a5b-bd2303c556dc");
        private readonly Guid documentId2 = Guid.Parse("e3f109ce-b095-4744-9141-b7640f561eda");
        private readonly Guid documentId3 = Guid.Parse("981a96c7-731d-4ac9-acd4-16b030697054");

        public AnnotationEventRepositoryTest()
        {

            _configuration = new Mock<IConfiguration>();

            _dataAccess = new DataAccessContextForTest(Guid.NewGuid(), _configuration.Object);
            _dataAccess.Database.EnsureDeleted();
            _dataAccess.Database.EnsureCreated();

            _repository = new AnnotationEventRepository(_dataAccess);
        }

        private async Task SeedDb()
        {           
            var annotations = new List<AnnotationEvent>
            {
                new AnnotationEvent { 
                    DocumentId = documentId1,
                    Action = Platform.Data.Enums.AnnotationAction.Create,
                    Details = "test1",
                    Author = UserFactory.GetUserByGivenId(Guid.NewGuid())                
                },
                new AnnotationEvent {
                    DocumentId = documentId2,
                    Action = Platform.Data.Enums.AnnotationAction.Create,
                    Details = "test2",
                    Author = UserFactory.GetUserByGivenId(Guid.NewGuid())
                },
                new AnnotationEvent {
                    DocumentId = documentId3,
                    Action = Platform.Data.Enums.AnnotationAction.Create,
                    Details = "test3",
                    Author = UserFactory.GetUserByGivenId(Guid.NewGuid())
                },
            };
            await _repository.CreateRange(annotations);
        }

        [Fact]
        public async Task GetAnnotationsByDocument_UsingInMemoryRepository()
        {
            // arrange
            await SeedDb();

            // act
            var result = await _repository.GetAnnotationsByDocument(documentId1, null);

            // assert
            Assert.NotNull(result);
            Assert.Contains(result, d => d.DocumentId == documentId1);
            Assert.Contains(result, d => d.Details == "test1");
            Assert.Contains(result, a => a.Action == Platform.Data.Enums.AnnotationAction.Create);
            Assert.Contains(result, a => a.Author.FirstName == "FirstNameUser1");
            Assert.Contains(result, a => a.Author.LastName == "LastNameUser1");
        }
    }
}
