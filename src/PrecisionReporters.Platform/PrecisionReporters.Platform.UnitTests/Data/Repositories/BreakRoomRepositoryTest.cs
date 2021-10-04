using Microsoft.Extensions.Configuration;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class BreakRoomRepositoryTest : BaseRepositoryTest<BreakRoom>
    {
        private readonly Mock<IConfiguration> _configuration;
        private static DataAccessContextForTest _dataAccess;
        private static BreakRoomRepository _repository;
        public BreakRoomRepositoryTest()
        {
            _configuration = new Mock<IConfiguration>();
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid(), _configuration.Object);
            _repository = new BreakRoomRepository(_dataAccess);
        }
}
}
