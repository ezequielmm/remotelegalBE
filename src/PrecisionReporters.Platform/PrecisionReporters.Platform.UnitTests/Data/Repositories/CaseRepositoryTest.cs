using Microsoft.Extensions.Configuration;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{

    public class CaseRepositoryTest:BaseRepositoryTest<Case>
    {
        private readonly Mock<IConfiguration> _configuration;
        private static DataAccessContextForTest _dataAccess;
        private static CaseRepository _repository;

        public CaseRepositoryTest() 
        {
            _configuration = new Mock<IConfiguration>();
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid(), _configuration.Object);
            _repository = new CaseRepository(_dataAccess);
        }
    }
}
