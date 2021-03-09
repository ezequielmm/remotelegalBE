using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class VerifyUserService : IVerifyUserService
    {
        private readonly IVerifyUserRepository _verifyUserRepository;

        public VerifyUserService(IVerifyUserRepository verifyUserRepository)
        {
            _verifyUserRepository = verifyUserRepository;
        }

        public async Task<VerifyUser> GetVerifyUserById(Guid id)
        {
            return await _verifyUserRepository.GetById(id, new []{ nameof(VerifyUser.User) });
        }

        public async Task<VerifyUser> GetVerifyUserByUserId(Guid userId, VerificationType? verificationType = null)
        {
            Expression<Func<VerifyUser, bool>> filter = x => x.User.Id.Equals(userId) && x.VerificationType == VerificationType.VerifyUser;
            if (verificationType.HasValue)
                filter = x => x.User.Id.Equals(userId) && x.VerificationType == verificationType;

            return await _verifyUserRepository.GetFirstOrDefaultByFilter(filter, new[] { nameof(VerifyUser.User) });
        }

        public async Task<VerifyUser> GetVerifyUserByEmail(string emailAddress, VerificationType? verificationType = null)
        {
            Expression<Func<VerifyUser, bool>> filter = x => x.User.EmailAddress.Equals(emailAddress) && x.VerificationType == VerificationType.VerifyUser;
            if (verificationType.HasValue)
                filter = x => x.User.EmailAddress.Equals(emailAddress) && x.VerificationType == verificationType;

            return await _verifyUserRepository.GetFirstOrDefaultByFilter(filter, new[] { nameof(VerifyUser.User) });
        }

        public async Task<VerifyUser> CreateVerifyUser(VerifyUser newVerifyUser)
        {
            return await _verifyUserRepository.Create(newVerifyUser);
        }

        public async Task<VerifyUser> UpdateVerifyUser(VerifyUser updatedVerifyUser)
        {
            return await _verifyUserRepository.Update(updatedVerifyUser);
        }
    }
}
