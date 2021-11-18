using System.Collections.Generic;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Domain.Enums;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ICognitoService
    {
        Task CreateAsync(User user);
        Task ConfirmUserAsync(string emailAddress);
        Task<Result<GuestToken>> LoginGuestAsync(User user);
        Task<Result> CheckUserExists(string emailAddress);
        Task<Result> DeleteUserAsync(User user);
        Task<bool> IsEnabled(string emailAddress);
        Task<Result> ResetPassword(User user);
        Task<Result<List<CognitoGroup>>> GetUserCognitoGroupList(string userEmail);
        Task<Result<GuestToken>> LoginUnVerifiedAsync(User user);
        Task DisableUser(User user);
    }
}
