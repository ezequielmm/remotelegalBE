using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Api.Helpers;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System.Threading.Tasks;
using System;

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
            var user = _userMapper.ToModel(createUserDto);

            var result = await _userService.SignUpAsync(user);
            if (result.IsFailed)
                return WebApiResponses.GetErrorResponse(result);

            return Ok(_userMapper.ToDto(result.Value));
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

        /// <summary>
        /// Gets the logged in user
        /// </summary>
        /// <returns>The current User logged in</returns>
        [HttpGet]
        [Route("currentUser")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
                return NotFound();
            return Ok(_userMapper.ToDto(user));
        }

        [HttpPost]
        [Route("forgotPassword")]
        public async Task<ActionResult<bool>> ForgotPassword(ForgotPasswordDto forgotPasswordDto)
        {
            var forgotPasswordResult = await _userService.ForgotPassword(forgotPasswordDto.Email);
            if (forgotPasswordResult.IsFailed)
                return WebApiResponses.GetErrorResponse(forgotPasswordResult);

            return Ok(true);
        }

        [HttpPost]
        [Route("verifyPasswordToken")]
        public async Task<ActionResult<VerifyForgotPasswordOutputDto>> VerifyForgotPassword(VerifyForgotPasswordDto verifyUseRequestDto)
        {
            var verifyForgotPasswordResult = await _userService.VerifyForgotPassword(verifyUseRequestDto.VerificationHash);
            if (verifyForgotPasswordResult.IsFailed)
                return WebApiResponses.GetErrorResponse(verifyForgotPasswordResult);

            return Ok(new VerifyForgotPasswordOutputDto { Email = verifyForgotPasswordResult.Value });
        }

        [HttpPut]
        [Route("changePassword")]
        public async Task<ActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var resetPasswordResult = await _userService.ResetPassword(resetPasswordDto.VerificationHash, resetPasswordDto.Password); ;
            if (resetPasswordResult.IsFailed)
                return WebApiResponses.GetErrorResponse(resetPasswordResult);

            return Ok();
        }
    }
}
