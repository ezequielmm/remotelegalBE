using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Threading.Tasks;
using FluentResults;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IUserService
    {
        Task<Result<User>> SignUpAsync(User user);
        Task<VerifyUser> VerifyUser(Guid verifyuserId);
        Task ResendVerificationEmailAsync(string email);
        Task<User> GetUserByEmail(string email);
    }
}
