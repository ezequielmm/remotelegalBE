using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class VerifyUserRepositoryTest : BaseRepositoryTest<VerifyUser>
    {
        private static DataAccessContextForTest _dataAccess;
        private static VerifyUserRepository _repository;
        public VerifyUserRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new VerifyUserRepository(_dataAccess);
        }
}
}
