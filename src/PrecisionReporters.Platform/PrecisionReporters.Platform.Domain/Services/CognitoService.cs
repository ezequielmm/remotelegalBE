using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Shared.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Domain.Enums;

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
                    await CreateVerifiedUserAccount(user, user.IsGuest);
                    await AddUserToGroup(user, _cognitoConfiguration.GuestUsersGroup);
                } else
                {
                    // Register the user using Cognito
                    await CreateVerifiedUserAccount(user, user.IsGuest);
                    await AddUserToGroup(user, _cognitoConfiguration.UnVerifiedUsersGroup);
                    // Disable the user until they confirm their email address
                    await DisableUser(user);
                }
            }
            catch (Exception ex)
            {
                throw new AmazonCognitoIdentityProviderException(new Exception("Failed to Register user on Aws Cognito", ex));
            }
        }

        public async Task ConfirmUserAsync(string emailAddress)
        {
            // Confirm SignUp
            await EnableUser(emailAddress);
            // Remove the user from the UnverifiedUsers group
            await RemoveUserFromGroup(emailAddress, _cognitoConfiguration.UnVerifiedUsersGroup);
        }

        private async Task CreateVerifiedUserAccount(User user, bool isGuest = true)
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

            var userPassword = isGuest ? _cognitoConfiguration.GuestUsersPass : user.Password;
            await SetPassword(user.EmailAddress, userPassword);
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

        private async Task AddUserToGroup(User user, string groupName)
        {
            var adminAddUserToGroupRequest = new AdminAddUserToGroupRequest
            {
                GroupName = groupName,
                Username = user.EmailAddress,
                UserPoolId = _cognitoConfiguration.UserPoolId
            };

            await _cognitoClient.AdminAddUserToGroupAsync(adminAddUserToGroupRequest);
        }

        private async Task RemoveUserFromGroup(string emailAddress, string groupName)
        {
            var removeUserFromGroupRequest = new AdminRemoveUserFromGroupRequest
            {
                Username = emailAddress,
                UserPoolId = _cognitoConfiguration.UserPoolId,
                GroupName = groupName
            };
            await _cognitoClient.AdminRemoveUserFromGroupAsync(removeUserFromGroupRequest);
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

        public async Task DisableUser(User user)
        {
            try
            {
                var adminDisableUserRequest = new AdminDisableUserRequest
                {
                    Username = user.EmailAddress,
                    UserPoolId = _cognitoConfiguration.UserPoolId
                };
                await _cognitoClient.AdminDisableUserAsync(adminDisableUserRequest);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to Disable user '{UserEmail}' on Aws Cognito", user.EmailAddress);
                throw new AmazonCognitoIdentityProviderException(new Exception("Failed to Disable user on Aws Cognito", ex));
            }
        }

        private async Task EnableUser(string emailAddress)
        {
            try
            {
                var adminEnableUser = new AdminEnableUserRequest
                {
                    Username = emailAddress,
                    UserPoolId = _cognitoConfiguration.UserPoolId
                };
                await _cognitoClient.AdminEnableUserAsync(adminEnableUser);

                var request = new AdminGetUserRequest
                {
                    Username = emailAddress,
                    UserPoolId = _cognitoConfiguration.UserPoolId
                };

                var registeredUserResult = await _cognitoClient.AdminGetUserAsync(request);
                if (registeredUserResult.UserStatus != UserStatusType.CONFIRMED)
                {
                    var confirmSignUp = new AdminConfirmSignUpRequest
                    {
                        Username = emailAddress,
                        UserPoolId = _cognitoConfiguration.UserPoolId
                    };

                    await _cognitoClient.AdminConfirmSignUpAsync(confirmSignUp);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to Enable user '{UserEmail}' on Aws Cognito", emailAddress);
                throw new AmazonCognitoIdentityProviderException(new Exception("Failed to Enable user on Aws Cognito", ex));
            }
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
                if (!await IsEnabled(user.EmailAddress))
                {
                    // If user wasn't confirmed we need to confirm it at this point
                    await ConfirmUserAsync(user.EmailAddress); 
                }

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

        public async Task<Result<GuestToken>> LoginUnVerifiedAsync(User user)
        {
            // Temporary verify user
            try
            {
                await EnableUser(user.EmailAddress);

                var cognito = new AmazonCognitoIdentityProviderClient();
                var request = new AdminInitiateAuthRequest
                {
                    UserPoolId = _cognitoConfiguration.UserPoolId,
                    ClientId = _cognitoConfiguration.UnVerifiedClientId,
                    AuthFlow = AuthFlowType.ADMIN_USER_PASSWORD_AUTH
                };
                request.AuthParameters.Add("USERNAME", user.EmailAddress);
                request.AuthParameters.Add("PASSWORD", user.Password);

                var response = await cognito.AdminInitiateAuthAsync(request);

                var token = new GuestToken
                {
                    IdToken = response.AuthenticationResult.IdToken
                };

                return Result.Ok(token);
            }
            catch (Exception ex)
            {
                await DisableUser(user);
                _log.LogWarning(ex, "Fail login for the unverified user {UserEmail}", user.EmailAddress);
                return Result.Fail(new InvalidInputError(ex.Message));
            }
            
        }

        /// <summary>
        /// Get a list of all the groups the user is a member of
        /// </summary>
        /// <param name="userEmail">Email of the user to look up</param>
        /// <returns>List of groups the user is a member of</returns>
        public async Task<Result<List<CognitoGroup>>> GetUserCognitoGroupList(string userEmail)
        {
            var request = new AdminListGroupsForUserRequest()
            {
                Username = userEmail,
                UserPoolId = _cognitoConfiguration.UserPoolId
            };
            var response = await _cognitoClient.AdminListGroupsForUserAsync(request);

            if (!response.Groups.Any())
                return Result.Fail(new InvalidInputError());

            return Result.Ok(response.Groups.Select(x => Enum.Parse<CognitoGroup>(x.GroupName)).ToList());
        }

        private async Task GetAllUnconfirmedUsers()
        {
            var request1 = new ListUsersRequest
            {
                UserPoolId = _cognitoConfiguration.UserPoolId,
                Filter = "cognito:user_status = \"UNCONFIRMED\"",
                AttributesToGet = new List<string> { "email" },
            };
            var list = await _cognitoClient.ListUsersAsync(request1);
        }
    }
}
