using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Api.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IMapper<User, UserDto, CreateUserDto> _userMapper;

        public UsersController(IUserService userService, IMapper<User, UserDto, CreateUserDto> userMapper)
        {
            _userService = userService;
            _userMapper = userMapper;
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="createUserDto"></param>
        /// <returns>Newly created UserDto</returns>
        [HttpPost]
        public async Task<ActionResult<UserDto>> SignUpAsync(CreateUserDto createUserDto)
        {
            var result = new User();
            try
            {
                var user = _userMapper.ToModel(createUserDto);
                result = await _userService.SignUpAsync(user);                
            }
            catch(UserAlreadyExistException ex)
            {
                return Conflict();
            }

            return Ok(_userMapper.ToDto(result));
        }

        /// <summary>
        /// Verify User by Email link
        /// </summary>
        /// <param name="verifyUseRequestDto"></param>
        /// <returns>Verify Action Result code</returns>
        [HttpPost]
        [Route("verifyUser")]
        public async Task<IActionResult> VerifyUserAsync(VerifyUseRequestDto verifyUseRequestDto)
        {
            await _userService.VerifyUser(verifyUseRequestDto.VerificationHash);
            return Ok();
        }

        /// <summary>
        /// Resend Email Verification
        /// </summary>
        /// <param name="resendEmailRequestDto"></param>
        /// <returns>Email Action Result code</returns>
        [HttpPost]
        [Route("resendVerificationEmail")]
        public async Task<IActionResult> ResendVerificationEmailAsync(ResendEmailRequestDto dto)
        {
            await _userService.ResendVerificationEmailAsync(dto.EmailAddress);
            return Ok();
        }
    }
}
