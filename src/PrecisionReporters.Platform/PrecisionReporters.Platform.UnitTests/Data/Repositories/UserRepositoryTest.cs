using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class UserRepositoryTest : BaseRepositoryTest<User>
    {
        private static DataAccessContextForTest _dataAccess;
        private static UserRepository _repository;
        public UserRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new UserRepository(_dataAccess);
        }
}
}
