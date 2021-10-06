using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class ActivityHistoryRepositoryTest : BaseRepositoryTest<ActivityHistory>
    {

        private static DataAccessContextForTest _dataAccess;
        private static ActivityHistoryRepository _repository;
        public ActivityHistoryRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new ActivityHistoryRepository(_dataAccess);
        }
    }
}
