using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IVerifyUserService
    {
        Task<VerifyUser> GetVerifyUserById(Guid id);
        Task<VerifyUser> GetVerifyUserByUserId(Guid userId);
        Task<VerifyUser> CreateVerifyUser(VerifyUser newVerifyUser);
        Task<VerifyUser> UpdateVerifyUser(VerifyUser updatedVerifyUser);
    }
}
