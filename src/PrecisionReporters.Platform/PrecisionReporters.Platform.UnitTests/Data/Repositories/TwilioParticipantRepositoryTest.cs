using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class TwilioParticipantRepositoryTest : BaseRepositoryTest<TwilioParticipant>
    {
        private static DataAccessContextForTest _dataAccess;
        private static TwilioParticipantRepository _repository;
        public TwilioParticipantRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new TwilioParticipantRepository(_dataAccess);
        }
}
}
