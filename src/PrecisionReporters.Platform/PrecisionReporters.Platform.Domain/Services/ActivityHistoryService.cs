using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class ActivityHistoryService:IActivityHistoryService
    {
        private readonly IActivityHistoryRepository _activityHistoryRepository;
        public ActivityHistoryService(IActivityHistoryRepository activityHistoryRepository)
        {
            _activityHistoryRepository = activityHistoryRepository;
        }
    }
}
