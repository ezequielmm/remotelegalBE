using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _log;
        private readonly IUserRepository _userRepository;
        private readonly ICognitoService _cognitoService;
        private readonly IAwsEmailService _awsEmailService;
        private readonly IVerifyUserService _verifyUserService;
        private readonly ITransactionHandler _transactionHandler;
        private readonly UrlPathConfiguration _urlPathConfiguration;
        private readonly VerificationLinkConfiguration _verificationLinkConfiguration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper<User, UserDto, CreateUserDto> _userMapper;

        public UserService(ILogger<UserService> log,
            IUserRepository userRepository,
            ICognitoService cognitoService,
            IAwsEmailService awsEmailService,
            IVerifyUserService verifyUserService,
            ITransactionHandler transactionHandler,
            IOptions<UrlPathConfiguration> urlPathConfiguration,
            IOptions<VerificationLinkConfiguration> verificationLinkConfiguration,
            IHttpContextAccessor httpContextAccessor, IMapper<User, UserDto, CreateUserDto> userMapper)
        {
            _log = log;
            _userRepository = userRepository;
            _cognitoService = cognitoService;
            _awsEmailService = awsEmailService;
            _verifyUserService = verifyUserService;
            _transactionHandler = transactionHandler;
            _urlPathConfiguration = urlPathConfiguration.Value;
            _verificationLinkConfiguration = verificationLinkConfiguration.Value;
            _httpContextAccessor = httpContextAccessor;
            _userMapper = userMapper;
        }

        public async Task<Result<User>> SignUpAsync(User user)
        {
            var userData = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress.Equals(user.EmailAddress));

            if (userData == null)
                return await CreateUser(user);

            if (!userData.IsGuest)
                return Result.Fail(new ResourceConflictError($"User with email {user.EmailAddress} already exists."));

            user.Id = userData.Id;
            user.IsGuest = false;
            user.CreationDate = userData.CreationDate;
            return await UpdateGuestToUser(user);
        }

        private async Task SendEmailVerification(User user, VerifyUser verifyUser)
        {
            if (user.IsGuest)
                return;

            var emailData = GetTemplate(user, verifyUser);
            await _awsEmailService.SetTemplateEmailRequest(emailData);
        }

        public async Task<Result<VerifyUser>> VerifyUser(Guid verifyuserId)
        {
            var verifyUser = await _verifyUserService.GetVerifyUserById(verifyuserId);
            if (verifyUser == null)
                return Result.Fail(new InvalidInputError("Invalid Verification Code"));

            var checkVerficationResult = CheckVerification(verifyUser);
            if (checkVerficationResult.IsFailed)
                return checkVerficationResult;

            await _cognitoService.ConfirmUserAsync(verifyUser.User.EmailAddress);

            verifyUser.IsUsed = true;
            var result = await _verifyUserService.UpdateVerifyUser(verifyUser);

            return Result.Ok(result);
        }

        public async Task ResendVerificationEmailAsync(string email)
        {
            var user = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress.Equals(email));
            var verifyUser = await _verifyUserService.GetVerifyUserByUserId(user.Id);
            var emailData = GetTemplate(user, verifyUser);
            await _awsEmailService.SetTemplateEmailRequest(emailData);
        }

        public async Task<Result<User>> GetUserByEmail(string email)
        {
            var user = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress == email);

            return user == null ? Result.Fail<User>(new ResourceNotFoundError($"User with email {email} not found.")) : Result.Ok(user);
        }

        public async Task<List<User>> GetUsersByFilter(Expression<Func<User, bool>> filter = null, string[] include = null)
        {
            return await _userRepository.GetByFilter(filter, include);
        }

        public async Task<Result<UserFilterResponseDto>> GetUsersByFilter(UserFilterDto filterDto)
        {
            var includes = new[] { nameof(User.VerifiedUsers) };

            var orderByQuery = GetUsersOrderBy(filterDto);
            var paginationResult = await _userRepository.GetByFilterPagination(null, orderByQuery.Compile(), includes, filterDto.Page, filterDto.PageSize);

            var response = new UserFilterResponseDto
            {
                Total = paginationResult.Item1,
                Page = filterDto.Page,
                NumberOfPages = (paginationResult.Item1 + filterDto.PageSize - 1) / filterDto.PageSize,
                Users = paginationResult.Item2.Select(x => _userMapper.ToDto(x)).ToList()
            };
            return Result.Ok(response);
        }

        private Expression<Func<IQueryable<User>, IOrderedQueryable<User>>> GetUsersOrderBy(UserFilterDto filterDto)
        {
            Expression<Func<User, object>> orderBy = filterDto.SortedField switch
            {
                UserSortField.FirstName => x => x.FirstName,
                UserSortField.LastName => x => x.LastName,
                UserSortField.Email => x => x.EmailAddress,
                UserSortField.AccountCreationDate => x => x.CreationDate,
                UserSortField.AccountVerifiedDate => x => x.VerifiedUsers.FirstOrDefault(y => y.VerificationType == VerificationType.VerifyUser),
                _ => x => x.LastName,
            };
            Expression<Func<User, object>> orderByThen = x => x.FirstName;
            Expression<Func<IQueryable<User>, IOrderedQueryable<User>>> orderByQuery = null;

                if (filterDto.SortDirection == null || filterDto.SortDirection == SortDirection.Ascend)
                {
                    orderByQuery = d => d.OrderBy(orderBy).ThenBy(orderByThen);
                }
                else
                {
                    orderByQuery = d => d.OrderByDescending(orderBy).ThenByDescending(orderByThen);
                }
            return orderByQuery;
        }

        private Result CheckVerification(VerifyUser verifyUser)
        {
            var expirationTime = int.Parse(_verificationLinkConfiguration.ExpirationTime);

            if (verifyUser.CreationDate < DateTime.UtcNow.AddHours(-expirationTime) || verifyUser.IsUsed)
            {
                _log.LogWarning(ApplicationConstants.VerificationCodeException);
                return Result.Fail(new Error(ApplicationConstants.VerificationCodeException));
            }

            return Result.Ok();
        }

        private async Task<Result<User>> CreateUser(User user)
        {
            User newUser = null;
            VerifyUser verifyUser = null;
            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                newUser = await _userRepository.Create(user);
                await _cognitoService.CreateAsync(user);
                verifyUser = await SaveVerifyUser(newUser, VerificationType.VerifyUser);
            });

            if (transactionResult.IsFailed)
                return transactionResult;

            await SendEmailVerification(newUser, verifyUser);

            return Result.Ok(newUser);
        }
        private async Task<VerifyUser> SaveVerifyUser(User user, VerificationType verificationType)
        {
            if (user.IsGuest)
                return null;

            var verifyUser = new VerifyUser
            {
                User = user,
                IsUsed = false,
                VerificationType = verificationType
            };

            return await _verifyUserService.CreateVerifyUser(verifyUser);
        }

        private EmailTemplateInfo GetTemplate(User user, VerifyUser verifyUser)
        {
            var template = new EmailTemplateInfo { EmailTo = new List<string> { user.EmailAddress }, TemplateName = verifyUser.VerificationType.GetDescription() };

            switch (verifyUser.VerificationType)
            {
                case VerificationType.VerifyUser:
                    template.TemplateData = new Dictionary<string, string>
                        {
                            { "user-name", user.FirstName },
                            { "verification-link",  $"{_urlPathConfiguration.FrontendBaseUrl}{_urlPathConfiguration.VerifyUserUrl}{verifyUser.Id}" }
                        };
                    break;
                case VerificationType.ForgotPassword:
                    template.TemplateData = new Dictionary<string, string>
                        {
                            { "user-name", user.FirstName },
                            { "resetpassword-link",  $"{_urlPathConfiguration.FrontendBaseUrl}{_urlPathConfiguration.ForgotPasswordUrl}{verifyUser.Id}" }
                        };
                    break;
            }

            return template;
        }

        //This method is only meant to get the logged in User
        //TODO for future increments add User value into a cache service
        public async Task<User> GetCurrentUserAsync()
        {
            var email = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
            var user = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress == email);

            return user;
        }

        public async Task<Result<GuestToken>> LoginGuestAsync(string emailAddress)
        {
            var userResult = await GetUserByEmail(emailAddress);
            if (userResult.IsFailed)
                return userResult.ToResult<GuestToken>();
            if (!userResult.Value.IsGuest)
                return Result.Fail(new InvalidInputError("Invalid user"));

            return await _cognitoService.LoginGuestAsync(userResult.Value);
        }

        public async Task<Result<User>> AddGuestUser(User user)
        {
            var userData = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress.Equals(user.EmailAddress));
            var cognitoUser = await _cognitoService.CheckUserExists(user.EmailAddress);

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                if (userData == null)
                    userData = await _userRepository.Create(user);

                if (cognitoUser.IsFailed)
                    await _cognitoService.CreateAsync(userData);
            });

            if (transactionResult.IsFailed)
                return transactionResult;

            return Result.Ok(userData);
        }

        public async Task RemoveGuestParticipants(List<Participant> participants)
        {
            foreach (var participant in participants?.Where(x => x.User != null && x.User.IsGuest))
            {
                var userExists = await _cognitoService.CheckUserExists(participant.User.EmailAddress);
                if (userExists.IsSuccess)
                    await _cognitoService.DeleteUserAsync(participant.User);
            }
        }

        public async Task<Result> ForgotPassword(ForgotPasswordDto forgotPasswordDto)
        {
            var userResult = await GetUserByEmail(forgotPasswordDto.Email);

            if (userResult.IsFailed)
            {
                _log.LogError("Invalid user");
                return Result.Ok();
            }

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                var verifyForgotPassword = await SaveVerifyUser(userResult.Value, VerificationType.ForgotPassword);
                await SendEmailVerification(userResult.Value, verifyForgotPassword);
            });

            if (transactionResult.IsFailed)
                return transactionResult;

            return Result.Ok();
        }

        public async Task<Result<string>> VerifyForgotPassword(VerifyForgotPasswordDto verifyUseRequestDto)
        {
            var verifyUser = await _verifyUserService.GetVerifyUserById(verifyUseRequestDto.VerificationHash);
            if (verifyUser == null)
                return Result.Fail(new InvalidInputError("Invalid Verification Code"));

            var checkVerficationResult = CheckVerification(verifyUser);
            if (checkVerficationResult.IsFailed)
                return checkVerficationResult;

            return Result.Ok(verifyUser.User.EmailAddress);
        }

        public async Task<Result> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var verifyUser = await _verifyUserService.GetVerifyUserById(resetPasswordDto.VerificationHash);

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                verifyUser.User.Password = resetPasswordDto.Password;
                verifyUser.IsUsed = true;

                await _cognitoService.ResetPassword(verifyUser.User);
                await _verifyUserService.UpdateVerifyUser(verifyUser);
            });

            if (transactionResult.IsFailed)
                return transactionResult;

            return Result.Ok();
        }

        private async Task<Result<User>> UpdateGuestToUser(User userToUpdate)
        {
            var cognitoUser = await _cognitoService.CheckUserExists(userToUpdate.EmailAddress);
            User updatedUser = null;
            VerifyUser verifyUser = null;

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                updatedUser = await _userRepository.Update(userToUpdate);

                if (cognitoUser.IsSuccess)
                    await _cognitoService.DeleteUserAsync(updatedUser);

                await _cognitoService.CreateAsync(updatedUser);
                verifyUser = await SaveVerifyUser(updatedUser, VerificationType.VerifyUser);
            });

            if (transactionResult.IsFailed)
                return transactionResult;

            await SendEmailVerification(updatedUser, verifyUser);

            return Result.Ok(updatedUser);
        }

    }
}
