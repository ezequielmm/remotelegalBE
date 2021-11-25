using FluentResults;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IChatService
    {
        Task<Result<JoinChatDto>> ManageChatParticipant(Guid depositionId);
    }
}
