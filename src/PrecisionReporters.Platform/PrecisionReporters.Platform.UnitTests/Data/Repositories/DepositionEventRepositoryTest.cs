using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class DepositionEventRepositoryTest : BaseRepositoryTest<DepositionEvent>
    {
        private static DataAccessContextForTest _dataAccess;
        private static DepositionEventRepository _repository;
        public DepositionEventRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new DepositionEventRepository(_dataAccess);
        }
}
}
