using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq.Expressions;
using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IUserService
    {
        Task<Result<User>> SignUpAsync(User user);
        Task<Result<VerifyUser>> VerifyUser(Guid verifyuserId);
        Task ResendVerificationEmailAsync(string email);
        Task<Result<User>> GetUserByEmail(string email);
        Task<List<User>> GetUsersByFilter(Expression<Func<User, bool>> filter = null, string[] include = null);
        Task<Result<UserFilterResponseDto>> GetUsersByFilter(UserFilterDto filterDto);
        Task<User> GetCurrentUserAsync();
        Task<Result<GuestToken>> LoginGuestAsync(string emailAddress);
        Task<Result<GuestToken>> LoginUnverifiedAsync(User unverifiedUser);
        Task<Result<User>> AddGuestUser(User user);
        Task RemoveGuestParticipants(List<Participant> participants);
        Task<Result> ForgotPassword(ForgotPasswordDto forgotPasswordDto);
        Task<Result> ResetPassword(ResetPasswordDto resetPasswordDto);
        Task<Result<string>> VerifyForgotPassword(VerifyForgotPasswordDto verifyUseRequestDto);
        Task<bool> CheckUserIsVerified(string emailAddress);
        Task DisableUnverifiedParticipants(List<Participant> participants);
    }
}
