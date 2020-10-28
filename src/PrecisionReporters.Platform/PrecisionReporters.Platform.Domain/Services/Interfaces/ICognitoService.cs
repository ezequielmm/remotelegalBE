using Amazon.CognitoIdentityProvider.Model;
using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ICognitoService
    {
        Task<SignUpResponse> CreateAsync(User user);
        Task<AdminConfirmSignUpResponse> ConfirmUserAsync(string emailAddress);
    }
}
