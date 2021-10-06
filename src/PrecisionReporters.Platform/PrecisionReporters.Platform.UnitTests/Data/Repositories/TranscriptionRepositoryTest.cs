using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class TranscriptionRepositoryTest : BaseRepositoryTest<Transcription>
    {
        private static DataAccessContextForTest _dataAccess;
        private static TranscriptionRepository _repository;
        public TranscriptionRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new TranscriptionRepository(_dataAccess);
        }
}
}
