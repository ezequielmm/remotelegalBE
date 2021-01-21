using System;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IBreakRoomService
    {
        Task<Result<BreakRoom>> GetBreakRoomById(Guid id, string[] include = null);
        Task<Result<string>> JoinBreakRoom(Guid id);
        Task<Result<BreakRoom>> LockBreakRoom(Guid breakRoomId, bool lockRoom);
    }
}
