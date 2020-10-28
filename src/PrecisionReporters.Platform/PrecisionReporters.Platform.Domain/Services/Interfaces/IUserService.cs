using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> SignUpAsync(User user);
        Task<VerifyUser> VerifyUser(Guid verifyuserId);
        Task ResendVerificationEmailAsync(string email);
    }
}
