using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class ParticipantRepositoryTest : BaseRepositoryTest<Participant>
    {
        private static DataAccessContextForTest _dataAccess;
        private static ParticipantRepository _repository;
        public ParticipantRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new ParticipantRepository(_dataAccess);
        }
}
}
