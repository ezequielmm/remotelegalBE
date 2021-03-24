using PrecisionReporters.Platform.Domain.Dtos;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ITwilioCallbackService
    {
        Task<Result<Room>> UpdateStatusCallback(RoomCallbackDto roomEvent);
    }
}
