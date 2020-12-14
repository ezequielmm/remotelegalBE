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
using System.Threading.Tasks;
using System.Linq.Expressions;

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
        public UserService(ILogger<UserService> log, IUserRepository userRepository, ICognitoService cognitoService, IAwsEmailService awsEmailService, IVerifyUserService verifyUserService, ITransactionHandler transactionHandler, IOptions<UrlPathConfiguration> urlPathConfiguration)
        {
            _log = log;
            _userRepository = userRepository;
            _cognitoService = cognitoService;
            _awsEmailService = awsEmailService;
            _verifyUserService = verifyUserService;
            _transactionHandler = transactionHandler;
            _urlPathConfiguration = urlPathConfiguration.Value;
        }

        public async Task<Result<User>> SignUpAsync(User user)
        {
            var userData = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress.Equals(user.EmailAddress));
            if (userData != null)
                return Result.Fail(new ResourceConflictError($"User with email {user.EmailAddress} already exists."));


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

            var verificationLink = $"{_urlPathConfiguration.FrontendBaseUrl}{_urlPathConfiguration.VerifyUserUrl}{verifyUser.Id}";
            var emailData = await SetVerifyEmailTemplate(user.EmailAddress, user.FirstName, verificationLink);
            await _awsEmailService.SetTemplateEmailRequest(emailData);

            return Result.Ok(newUser);
        }

        public async Task<VerifyUser> VerifyUser(Guid verifyuserId)
        {
            var verifyUser = await _verifyUserService.GetVerifyUserById(verifyuserId);

            if (verifyUser.CreationDate < DateTime.UtcNow.AddDays(-1) || verifyUser.IsUsed)
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

        private async Task<VerifyUser> SaveVerifyUser(User user)
        {
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
    }
}
