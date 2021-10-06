using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class DocumentRepositoryTest : BaseRepositoryTest<Document>
    {
        private static DataAccessContextForTest _dataAccess;
        private static DocumentRepository _repository;
        public DocumentRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new DocumentRepository(_dataAccess);
        }
}
}
