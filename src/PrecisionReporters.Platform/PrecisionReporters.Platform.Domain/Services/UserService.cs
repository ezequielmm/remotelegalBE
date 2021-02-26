using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
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
        private readonly ClaimsPrincipal _principal;

        public UserService(ILogger<UserService> log, IUserRepository userRepository, ICognitoService cognitoService, IAwsEmailService awsEmailService,
            IVerifyUserService verifyUserService, ITransactionHandler transactionHandler, IOptions<UrlPathConfiguration> urlPathConfiguration, IOptions<VerificationLinkConfiguration> verificationLinkConfiguration, ClaimsPrincipal principal)
        {
            _log = log;
            _userRepository = userRepository;
            _cognitoService = cognitoService;
            _awsEmailService = awsEmailService;
            _verifyUserService = verifyUserService;
            _transactionHandler = transactionHandler;
            _urlPathConfiguration = urlPathConfiguration.Value;
            _verificationLinkConfiguration = verificationLinkConfiguration.Value;
            _principal = principal;
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

            var verificationLink = $"{_urlPathConfiguration.FrontendBaseUrl}{_urlPathConfiguration.VerifyUserUrl}{verifyUser.Id}";
            var emailData = await SetVerifyEmailTemplate(user.EmailAddress, user.FirstName, verificationLink);
            await _awsEmailService.SetTemplateEmailRequest(emailData);
        }

        public async Task<VerifyUser> VerifyUser(Guid verifyuserId)
        {
            var verifyUser = await _verifyUserService.GetVerifyUserById(verifyuserId);
            var expirationTime = int.Parse(_verificationLinkConfiguration.ExpirationTime);

            if (verifyUser.CreationDate < DateTime.UtcNow.AddHours(-expirationTime) || verifyUser.IsUsed)
            {
                _log.LogWarning(ApplicationConstants.VerificationCodeException);

                // TODO: return a result and remove this exception from the project
                throw new HashExpiredOrAlreadyUsedException(ApplicationConstants.VerificationCodeException);
            }

            await _cognitoService.ConfirmUserAsync(verifyUser.User.EmailAddress);

            verifyUser.IsUsed = true;
            var result = await _verifyUserService.UpdateVerifyUser(verifyUser);

            return result;
        }

        public async Task ResendVerificationEmailAsync(string email)
        {
            var user = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress.Equals(email));
            var verifyUser = await _verifyUserService.GetVerifyUserByUserId(user.Id);

            var verificationLink = $"{_urlPathConfiguration.FrontendBaseUrl}{_urlPathConfiguration.VerifyUserUrl}{verifyUser.Id}";
            var emailData = await SetVerifyEmailTemplate(email, user.FirstName, verificationLink);
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

        private async Task<Result<User>> CreateUser(User user)
        {
            User newUser = null;
            VerifyUser verifyUser = null;
            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                newUser = await _userRepository.Create(user);
                await _cognitoService.CreateAsync(user);
                verifyUser = await SaveVerifyUser(newUser);
            });

            if (transactionResult.IsFailed)
                return transactionResult;

            await SendEmailVerification(newUser, verifyUser);

            return Result.Ok(newUser);
        }
        private async Task<VerifyUser> SaveVerifyUser(User user)
        {
            if (user.IsGuest)
                return null;

            var verifyUser = new VerifyUser
            {
                User = user,
                IsUsed = false
            };

            return await _verifyUserService.CreateVerifyUser(verifyUser);
        }

        private async Task<EmailTemplateInfo> SetVerifyEmailTemplate(string emailAddress, string firstName, string verificationLink)
        {
            return await Task.Run(() => new EmailTemplateInfo
            {
                EmailTo = new List<string> { emailAddress },
                TemplateName = ApplicationConstants.VerificationEmailTemplate,
                TemplateData = new Dictionary<string, string>
                {
                    { "user-name", firstName },
                    { "verification-link", verificationLink }
                }
            });
        }

        //This method is only meant to get the logged in User
        //TODO for future increments add User value into a cache service
        public async Task<User> GetCurrentUserAsync()
        {
            var email = _principal.FindFirstValue(ClaimTypes.Email);
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
                verifyUser = await SaveVerifyUser(updatedUser);
            });

            if (transactionResult.IsFailed)
                return transactionResult;

            await SendEmailVerification(updatedUser, verifyUser);

            return Result.Ok(updatedUser);
        }
    }
}
