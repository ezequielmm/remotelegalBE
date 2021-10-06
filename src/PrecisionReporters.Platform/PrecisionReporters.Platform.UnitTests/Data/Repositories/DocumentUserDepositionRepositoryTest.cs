using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class DocumentUserDepositionRepositoryTest : BaseRepositoryTest<DocumentUserDeposition>
    {
        private static DataAccessContextForTest _dataAccess;
        private static DocumentUserDepositionRepository _repository;
        public DocumentUserDepositionRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new DocumentUserDepositionRepository(_dataAccess);
        }
}
}
