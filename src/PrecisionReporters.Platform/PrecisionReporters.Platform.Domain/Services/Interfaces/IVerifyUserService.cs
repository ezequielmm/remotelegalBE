using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IVerifyUserService
    {
        Task<VerifyUser> GetVerifyUserById(Guid id);
        Task<VerifyUser> GetVerifyUserByUserId(Guid userId, VerificationType? verificationType = null);
        Task<VerifyUser> CreateVerifyUser(VerifyUser newVerifyUser);
        Task<VerifyUser> UpdateVerifyUser(VerifyUser updatedVerifyUser);
        Task<VerifyUser> GetVerifyUserByEmail(string emailAddress, VerificationType? verificationType = null);
    }
}
