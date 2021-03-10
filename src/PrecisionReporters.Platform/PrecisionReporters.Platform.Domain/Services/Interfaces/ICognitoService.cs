using Amazon.CognitoIdentityProvider.Model;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ICognitoService
    {
        Task CreateAsync(User user);
        Task<AdminConfirmSignUpResponse> ConfirmUserAsync(string emailAddress);
        Task<Result<GuestToken>> LoginGuestAsync(User user);
        Task<Result> CheckUserExists(string emailAddress);
        Task<Result> DeleteUserAsync(User user);
        Task<Result<bool>> IsEnabled(string emailAddress);
        Task<Result> ResetPassword(User user);
    }
}
