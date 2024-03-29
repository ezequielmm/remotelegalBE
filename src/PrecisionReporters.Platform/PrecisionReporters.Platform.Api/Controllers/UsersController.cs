﻿using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Helpers;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IDepositionService _depositionService;
        private readonly IMapper<User, UserDto, CreateUserDto> _userMapper;

        public UsersController(IUserService userService, IDepositionService depositionService, IMapper<User, UserDto, CreateUserDto> userMapper)
        {
            _userService = userService;
            _userMapper = userMapper;
            _depositionService = depositionService;
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

            var depositionResult = await _depositionService.UpdateParticipantOnExistingDepositions(result.Value);

            if (depositionResult.IsFailed)
                return WebApiResponses.GetErrorResponse(depositionResult);

            return Ok(_userMapper.ToDto(result.Value));
        }

        /// <summary>
        /// Verify User by Email link
        /// </summary>
        /// <param name="verifyUseRequestDto"></param>
        /// <returns>Verify Action Result code</returns>
        [HttpPost]
        [Route("verifyUser")]
        public async Task<ActionResult<ResultDto>> VerifyUserAsync(VerifyUseRequestDto verifyUseRequestDto)
        {
            var result = await _userService.VerifyUser(verifyUseRequestDto.VerificationHash);
            return Ok(new ResultDto { Success = result.IsSuccess });
        }

        /// <summary>
        /// Resend Email Verification
        /// </summary>
        /// <param name="resendEmailRequestDto"></param>
        /// <returns>Email Action Result code</returns>
        [HttpPost]
        [Route("resendVerificationEmail")]
        public async Task<ActionResult<ResultDto>> ResendVerificationEmailAsync(ResendEmailRequestDto dto)
        {
            await _userService.ResendVerificationEmailAsync(dto.EmailAddress);
            return Ok(new ResultDto { Success = true });
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

        /// <summary>
        /// Gets the logged in admin user
        /// </summary>
        /// <returns>The current Admin User logged in if succeeded</returns>
        [HttpGet]
        [Route("currentAdminUser")]
        public async Task<ActionResult<UserDto>> GetCurrentAdminUser()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
                return NotFound();
            if (!user.IsAdmin)
                return StatusCode(403);

            return Ok(_userMapper.ToDto(user));
        }

        [HttpGet]
        [Route("users")]
        public async Task<ActionResult<UserFilterResponseDto>> GetUsers([FromQuery] UserFilterDto filter)
        {
            // TODO: Remove this block after Authorization decorator refactoring
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
                return NotFound();
            if (!user.IsAdmin)
                return StatusCode(403);

            var userResponseResult = await _userService.GetUsersByFilter(filter);

            if (userResponseResult.IsFailed)
                return WebApiResponses.GetErrorResponse(userResponseResult);

            return Ok(userResponseResult.Value);
        }

        [HttpPost]
        [Route("forgotPassword")]
        public async Task<ActionResult<bool>> ForgotPassword(ForgotPasswordDto forgotPasswordDto)
        {
            var forgotPasswordResult = await _userService.ForgotPassword(forgotPasswordDto);
            if (forgotPasswordResult.IsFailed)
                return WebApiResponses.GetErrorResponse(forgotPasswordResult);

            return Ok(true);
        }

        [HttpPost]
        [Route("verifyPasswordToken")]
        public async Task<ActionResult<VerifyForgotPasswordOutputDto>> VerifyForgotPassword(VerifyForgotPasswordDto verifyUseRequestDto)
        {
            var verifyForgotPasswordResult = await _userService.VerifyForgotPassword(verifyUseRequestDto);
            if (verifyForgotPasswordResult.IsFailed)
                return WebApiResponses.GetErrorResponse(verifyForgotPasswordResult);

            return Ok(new VerifyForgotPasswordOutputDto { Email = verifyForgotPasswordResult.Value });
        }

        [HttpPut]
        [Route("changePassword")]
        public async Task<ActionResult<bool>> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var resetPasswordResult = await _userService.ResetPassword(resetPasswordDto);
            if (resetPasswordResult.IsFailed)
                return WebApiResponses.GetErrorResponse(resetPasswordResult);

            return Ok(true);
        }
    }
}
