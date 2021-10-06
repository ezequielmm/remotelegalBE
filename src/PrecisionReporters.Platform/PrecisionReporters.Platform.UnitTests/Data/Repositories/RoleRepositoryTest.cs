using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class RoleRepositoryTest : BaseRepositoryTest<Role>
    {
        private static DataAccessContextForTest _dataAccess;
        private static RoleRepository _repository;
        public RoleRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new RoleRepository(_dataAccess);
        }
}
}
