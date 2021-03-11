using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class CognitoService : ICognitoService
    {
        private readonly CognitoConfiguration _cognitoConfiguration;
        private readonly IAmazonCognitoIdentityProvider _cognitoClient;
        private readonly ILogger<CognitoService> _log;

        public CognitoService(IOptions<CognitoConfiguration> cognitoConfiguration, IAmazonCognitoIdentityProvider cognitoClient, ILogger<CognitoService> log)
        {
            _cognitoConfiguration = cognitoConfiguration.Value;
            _cognitoClient = cognitoClient;
            _log = log;
        }

        public async Task CreateAsync(User user)
        {
            try
            {
                if (user.IsGuest)
                {
                    await CreateVerifiedUserAccount(user);
                    await AddUserToGuestsGroup(user);
                } else
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
                    await _cognitoClient.SignUpAsync(signUpRequest);
                }
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

        private async Task CreateVerifiedUserAccount(User user)
        {
            var adminCreateUser = new AdminCreateUserRequest
            {
                UserPoolId = _cognitoConfiguration.UserPoolId,
                MessageAction = MessageActionType.SUPPRESS,
                DesiredDeliveryMediums = new List<string> { "EMAIL" },
                Username = user.EmailAddress,
                UserAttributes = new List<AttributeType> { new AttributeType { Name = "email", Value = user.EmailAddress } }
            };

            await _cognitoClient.AdminCreateUserAsync(adminCreateUser);

            await SetPassword(user.EmailAddress, _cognitoConfiguration.GuestUsersPass);
        }

        private async Task SetPassword(string email, string password)
        {
            var setPasswordRequest = new AdminSetUserPasswordRequest
            {
                Username = email,
                Password = password,
                Permanent = true,
                UserPoolId = _cognitoConfiguration.UserPoolId
            };
            await _cognitoClient.AdminSetUserPasswordAsync(setPasswordRequest);
        }

        private async Task AddUserToGuestsGroup(User user)
        {
            var adminAddUserToGroupRequest = new AdminAddUserToGroupRequest
            {
                GroupName = _cognitoConfiguration.GuestUsersGroup,
                Username = user.EmailAddress,
                UserPoolId = _cognitoConfiguration.UserPoolId
            };

            await _cognitoClient.AdminAddUserToGroupAsync(adminAddUserToGroupRequest);
        }

        public async Task<Result<GuestToken>> LoginGuestAsync(User user)
        {
            var cognito = new AmazonCognitoIdentityProviderClient();
            var request = new AdminInitiateAuthRequest
            {
                UserPoolId = _cognitoConfiguration.UserPoolId,
                ClientId = _cognitoConfiguration.GuestClientId,
                AuthFlow = AuthFlowType.ADMIN_USER_PASSWORD_AUTH
            };

            request.AuthParameters.Add("USERNAME", user.EmailAddress);
            request.AuthParameters.Add("PASSWORD", _cognitoConfiguration.GuestUsersPass);

            AdminInitiateAuthResponse response = await cognito.AdminInitiateAuthAsync(request);
            
            var token = new GuestToken
            {
                IdToken = response.AuthenticationResult.IdToken
            };
            return Result.Ok(token);
        }

        public async Task<Result> CheckUserExists(string emailAddress)
        {
            var request = new AdminGetUserRequest {
                Username = emailAddress,
                UserPoolId = _cognitoConfiguration.UserPoolId
            };
            try
            {
                var registeredUserResult = await _cognitoClient.AdminGetUserAsync(request);
                if (registeredUserResult == null)
                    return Result.Fail(new ResourceNotFoundError($"User {emailAddress} not found"));
            }
            catch
            {
                return Result.Fail(new ResourceNotFoundError($"User {emailAddress} not found"));
            }
            

            return Result.Ok();
        }

        public async Task<bool> IsEnabled(string emailAddress)
        {
            try
            {
                var request = new AdminGetUserRequest
                {
                    Username = emailAddress,
                    UserPoolId = _cognitoConfiguration.UserPoolId
                };

                var registeredUserResult = await _cognitoClient.AdminGetUserAsync(request);
                if (registeredUserResult == null)
                    return false;

                return registeredUserResult.Enabled;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, ex.Message);
                return false;
            }
        }

        public async Task<Result> ResetPassword(User user)
        {
            try
            {
                await SetPassword(user.EmailAddress, user.Password);
            }
            catch (Exception ex)
            {
                throw new AmazonCognitoIdentityProviderException(new Exception("Failed to Reset Password on Aws Cognito", ex));
            }

            return Result.Ok();
        }

        public async Task<Result> DeleteUserAsync(User user)
        {
            var userExistsResult = await CheckUserExists(user.EmailAddress);

            if (userExistsResult.IsFailed)
                return userExistsResult;

            var request = new AdminDeleteUserRequest
            {
                Username = user.EmailAddress,
                UserPoolId = _cognitoConfiguration.UserPoolId
            };
            await _cognitoClient.AdminDeleteUserAsync(request);

            return Result.Ok();
        }
    }
}
