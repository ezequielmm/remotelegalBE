using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class DeviceInfoRepositoryTest : BaseRepositoryTest<DeviceInfo>
    {
        private static DataAccessContextForTest _dataAccess;
        private static DeviceInfoRepository _repository;
        public DeviceInfoRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new DeviceInfoRepository(_dataAccess);
        }
}
}
