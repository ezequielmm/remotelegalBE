using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class CognitoService : ICognitoService
    {
        private readonly CognitoConfiguration _cognitoConfiguration;
        private readonly IAmazonCognitoIdentityProvider _cognitoClient;

        public CognitoService(IOptions<CognitoConfiguration> cognitoConfiguration, IAmazonCognitoIdentityProvider cognitoClient)
        {
            _cognitoConfiguration = cognitoConfiguration.Value;
            _cognitoClient = cognitoClient;
        }

        public async Task<SignUpResponse> CreateAsync(User user)
        {
            try
            {
                // Register the user using Cognito
                var signUpRequest = new SignUpRequest
                {
                    ClientId = _cognitoConfiguration.ClientId,
                    Password = user.Password,
                    Username = user.EmailAddress,
                };

                var emailAttribute = new AttributeType
                {
                    Name = "email",
                    Value = user.EmailAddress
                };

                signUpRequest.UserAttributes.Add(emailAttribute);

                return await _cognitoClient.SignUpAsync(signUpRequest);
            }
            catch (Exception ex)
            {
                throw new AmazonCognitoIdentityProviderException(new Exception("Failed to Register user on Aws Cognito", ex));
            }
        }

        public async Task<AdminConfirmSignUpResponse> ConfirmUserAsync(string emailAddress)
        {
            // Confirm SigunUp
            var confirmSignUp = new AdminConfirmSignUpRequest
            {
                Username = emailAddress,
                UserPoolId = _cognitoConfiguration.UserPoolId
            };

            return await _cognitoClient.AdminConfirmSignUpAsync(confirmSignUp);
        }
    }
}
