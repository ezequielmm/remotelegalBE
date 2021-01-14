using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentResults;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class AnnotationEventTests
    {
        private readonly Mock<IAnnotationEventRepository> _annotationEventRepositoryMock;
        private readonly AnnotationEventService _service;

        public AnnotationEventTests()
        {
            _annotationEventRepositoryMock = new Mock<IAnnotationEventRepository>();
            _service = new AnnotationEventService(_annotationEventRepositoryMock.Object);
        }

        [Fact]
        public async Task GetAnnotations_ShouldSearchWithProperFilters()
        {
            // Arrange
            var document = new Document
            {
                Id = Guid.NewGuid()
            };
            var annotationId = Guid.NewGuid();

            _annotationEventRepositoryMock
                .Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>()))
                .ReturnsAsync(new AnnotationEvent());

            // Act
            var result = await _service.GetDocumentAnnotations(document.Id, annotationId);

            // Assert
            _annotationEventRepositoryMock.Verify(x => x.GetByFilter(It.IsAny<Expression<Func<AnnotationEvent, object>>>(),
                It.IsAny<SortDirection>(),
                It.Is<Expression<Func<AnnotationEvent, bool>>>(x => x != null),
                It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GetAnnotations_ShouldReturnFail_IfAnnotationNotFound()
        {
            // Arrange
            var annotationId = Guid.NewGuid();
            var expectedError = $"annoitation with Id {annotationId} could not be found";
            _annotationEventRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<string[]>())).ReturnsAsync((AnnotationEvent)null);
            // Act
            var result = await _service.GetDocumentAnnotations(Guid.NewGuid(), annotationId);
            // Assert
            _annotationEventRepositoryMock.Verify(x => x.GetById(It.Is<Guid>(a => a == annotationId), It.IsAny<string[]>()), Times.Once);
            Assert.NotNull(result);
            Assert.IsType<Result<List<AnnotationEvent>>>(result);
            Assert.True(result.IsFailed);
            Assert.Contains(expectedError, result.Errors.Select(x => x.Message));
        }
    }
}
