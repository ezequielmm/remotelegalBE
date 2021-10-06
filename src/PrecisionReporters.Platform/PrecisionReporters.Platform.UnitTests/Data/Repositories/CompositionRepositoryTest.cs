using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class CompositionRepositoryTest : BaseRepositoryTest<Composition>
    {
        private static DataAccessContextForTest _dataAccess;
        private static CompositionRepository _repository;
        public CompositionRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new CompositionRepository(_dataAccess);
        }
}
}
