using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Mappers
{
    public class MemberMapperTest
    {
        private readonly MemberMapper _memberMapper;

        public MemberMapperTest()
        {
            _memberMapper = new MemberMapper();
        }

        [Fact]
        public void ToDto_ShouldReturnDto()
        {
            // Arrange
            var id = Guid.NewGuid();

            var model = new Member
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.MinValue,
                CaseId = Guid.NewGuid(),
                UserId = id,
                User = UserFactory.GetUserByGivenId(id),
                Case = new Case { 
                    Id = Guid.NewGuid()
                }

            };

            // Act
            var result = _memberMapper.ToDto(model);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model.Id, result.Id);
            Assert.Equal(model.CreationDate, result.CreationDate);
            Assert.Equal(model.Case.Id, result.CaseId);            
            Assert.Equal(model.User.Id, result.UserId);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithMemberDto()
        {
            // Arrange
            var id = Guid.NewGuid();

            var dto = new MemberDto
            {
                Id = Guid.NewGuid(),
                CreationDate = It.IsAny<DateTime>(),
                CaseId = Guid.NewGuid(),
                UserId = id
            };

            // Act
            var result = _memberMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Id, result.Id);
            Assert.Equal(dto.CreationDate.UtcDateTime, result.CreationDate);
            Assert.Equal(dto.CaseId, result.CaseId);
            Assert.Equal(dto.UserId, result.UserId);
        }

        [Fact]
        public void ToModel_ShouldNormalizeFields_WithCreateMemberDto()
        {
            // Arrange
            var id = Guid.NewGuid();

            var dto = new CreateMemberDto
            {
                UserId = id,
                CaseId = Guid.NewGuid()
            };

            // Act
            var result = _memberMapper.ToModel(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.CaseId, result.CaseId);
            Assert.Equal(dto.UserId, result.UserId);
        }
    }
}
