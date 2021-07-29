using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IActivityHistoryService
    {
        Task<Result> AddActivity(ActivityHistory activity, User user, Deposition deposition);
        Task<Result> UpdateUserSystemInfo(Guid depositionId, UserSystemInfo userSystemInfo, User user);
    }
}
