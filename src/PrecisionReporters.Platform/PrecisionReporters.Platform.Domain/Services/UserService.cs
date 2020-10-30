using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
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
        private readonly EmailConfiguration _emailConfiguration;
        private User _newUser;
        private VerifyUser _verifyUser;

        public UserService(ILogger<UserService> log, IUserRepository userRepository, ICognitoService cognitoService, IAwsEmailService awsEmailService, IVerifyUserService verifyUserService, ITransactionHandler transactionHandler, IOptions<UrlPathConfiguration> urlPathConfiguration, IOptions<EmailConfiguration> emailConfiguration)
        {
            _log = log;
            _userRepository = userRepository;
            _cognitoService = cognitoService;
            _awsEmailService = awsEmailService;
            _verifyUserService = verifyUserService;
            _transactionHandler = transactionHandler;
            _urlPathConfiguration = urlPathConfiguration.Value;
            _emailConfiguration = emailConfiguration.Value;
        }

        public async Task<User> SignUpAsync(User user)
        {
            var userData = await _userRepository.GetFirstOrDefaultByFilter(x => x.EmailAddress.Equals(user.EmailAddress));
            if (userData != null)
            {
                throw new UserAlreadyExistException(user.EmailAddress);
            }

            //TODO: Perform logic to commit transactions
            await _transactionHandler.RunAsync(async () =>
            {
                _newUser = await _userRepository.Create(user);
                await _cognitoService.CreateAsync(user);
                _verifyUser = await SaveVerifyUser(_newUser);
            });
            
            var verificationLink = $"{_urlPathConfiguration.FrontendBaseUrl}{_urlPathConfiguration.VerifyUserUrl}{_verifyUser.Id}";
            var emailData = SetVerifyEmailData(user.EmailAddress, user.FirstName, user.LastName, verificationLink);
            await _awsEmailService.SendEmailAsync(emailData, user.EmailAddress);
            return _newUser;
        }

        public async Task<VerifyUser> VerifyUser(Guid validationHash)
        {
            var verifyUser = await _verifyUserService.GetVerifyUserById(validationHash);

            if (verifyUser.CreationDate < DateTime.UtcNow.AddDays(-1) || verifyUser.IsUsed)
            {
                _log.LogWarning("Verification Code is already used or out of date");
                return null;
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
            var emailData = SetVerifyEmailData(user.EmailAddress, user.FirstName, user.LastName, verificationLink);
            await _awsEmailService.SendEmailAsync(emailData, email);
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

        private EmailTemplateInfo SetVerifyEmailData(string emailTo, string firstName, string lastName, string verificationLink)
        {
            var emailInfo = new EmailTemplateInfo
            {
                TemplateData = new List<string>
                {
                    $"{firstName} {lastName}",
                    emailTo,
                    verificationLink,
                    _emailConfiguration.EmailHelp
                },
                TemplateName = _emailConfiguration.VerifyTemplateName
            };
            return emailInfo;
        }
    }
}
