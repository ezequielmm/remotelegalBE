using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class RoomRepositoryTest : BaseRepositoryTest<Room>
    {
        private static DataAccessContextForTest _dataAccess;
        private static RoomRepository _repository;
        public RoomRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new RoomRepository(_dataAccess);
        }
}
}
