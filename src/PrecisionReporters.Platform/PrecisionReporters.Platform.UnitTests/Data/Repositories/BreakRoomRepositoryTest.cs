﻿using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using System;

namespace PrecisionReporters.Platform.UnitTests.Data.Repositories
{
    public class BreakRoomRepositoryTest : BaseRepositoryTest<BreakRoom>
    {
        private static DataAccessContextForTest _dataAccess;
        private static BreakRoomRepository _repository;
        public BreakRoomRepositoryTest()
        {
            _dataAccess = new DataAccessContextForTest(Guid.NewGuid());
            _repository = new BreakRoomRepository(_dataAccess);
        }
}
}
