using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class SystemSettingsRepositoryTest : BaseRepositoryTest<SystemSettings>
    {
        private static DataAccessContextForTest _dataAccess;
        private static SystemSettingsRepository _repository;
        public SystemSettingsRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new SystemSettingsRepository(_dataAccess);
        }
}
}
