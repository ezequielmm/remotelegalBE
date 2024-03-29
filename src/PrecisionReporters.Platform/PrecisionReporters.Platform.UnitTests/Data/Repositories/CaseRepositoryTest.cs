﻿using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{

    public class CaseRepositoryTest:BaseRepositoryTest<Case>
    {
        private static DataAccessContextForTest _dataAccess;
        private static CaseRepository _repository;

        public CaseRepositoryTest() 
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new CaseRepository(_dataAccess);
        }
    }
}
