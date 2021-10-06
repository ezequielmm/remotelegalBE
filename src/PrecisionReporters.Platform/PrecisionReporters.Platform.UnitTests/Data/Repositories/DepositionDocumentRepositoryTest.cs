using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class DepositionDocumentRepositoryTest : BaseRepositoryTest<DepositionDocument>
    {
        private static DataAccessContextForTest _dataAccess;
        private static DepositionDocumentRepository _repository;
        public DepositionDocumentRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new DepositionDocumentRepository(_dataAccess);
        }
}
}
