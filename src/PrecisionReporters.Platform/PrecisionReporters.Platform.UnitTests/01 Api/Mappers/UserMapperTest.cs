using System;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Api.Mappers
{
    public class UserMapperTest
    {
        private readonly IMapper<User, UserDto, CreateUserDto> _userMapper;
        private CreateUserDto _createUserDto;

        public UserMapperTest()
        {
            _userMapper = new UserMapper();
        }

        [Fact]
        public async Task MapToModel_ShouldReturn_NewUser()
        {
            _createUserDto = new CreateUserDto
            {
                FirstName = "TestUserName",
                LastName = "TestUserLastName",
                CompanyName = "TestCompanyName",
                CompanyAddress = "TestCompanyAddress",
                EmailAddress = "Test@test.com",
                Password = "TestPassword"
            };

            var user = _userMapper.ToModel(_createUserDto);

            Assert.Equal(_createUserDto.LastName, user.LastName);
            Assert.Equal(_createUserDto.FirstName, user.FirstName);
            Assert.Equal(_createUserDto.CompanyName, user.CompanyName);
            Assert.Equal(_createUserDto.CompanyAddress, user.CompanyAddress);
            Assert.Equal(_createUserDto.EmailAddress, user.EmailAddress);
            Assert.Equal(_createUserDto.Password, user.Password);
        }

        [Fact]
        public async Task MapToDto_ShouldReturn_NewUserDto()
        {
            var user = new User
            {
                Id = new Guid(),
                FirstName = "TestUserName",
                LastName = "TestUserLastName",
                CompanyName = "TestCompanyName",
                CompanyAddress = "TestCompanyAddress",
                EmailAddress = "Test@test.com",
                Password = "TestPassword"
            };

            var userDto = _userMapper.ToDto(user);

            Assert.Equal(userDto.LastName, user.LastName);
            Assert.Equal(userDto.FirstName, user.FirstName);
            Assert.Equal(userDto.CompanyName, user.CompanyName);
            Assert.Equal(userDto.CompanyAddress, user.CompanyAddress);
            Assert.Equal(userDto.EmailAddress, user.EmailAddress);
            Assert.Equal(userDto.Password, user.Password);
        }
    }
}
