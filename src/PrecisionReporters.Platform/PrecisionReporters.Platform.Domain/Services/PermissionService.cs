using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUserResourceRoleRepository _userResourceRoleRepository;
        private readonly IUserService _userService;

        public PermissionService(IRoleRepository roleRepository, IUserResourceRoleRepository userResourceRoleRepository, IUserService userService)
        {
            _roleRepository = roleRepository;
            _userResourceRoleRepository = userResourceRoleRepository;
            _userService = userService;
        }

        public async Task<Result> AddUserRole(Guid userId, Guid resourceId, ResourceType resourceType, RoleName roleName)
        {
            var role = await _roleRepository.GetFirstOrDefaultByFilter(r => r.Name == roleName);
            if (role == null)
                return Result.Fail(new ResourceNotFoundError());

            var newUserResourceRole = new UserResourceRole
            {
                ResourceId = resourceId,
                ResourceType = resourceType,
                RoleId = role.Id,
                UserId = userId
            };

            await _userResourceRoleRepository.Create(newUserResourceRole);
            return Result.Ok();
        }

        public async Task<bool> CheckUserHasPermissionForAction(string userEmail, ResourceType resourceType, Guid resourceId, ResourceAction resourceAction)
        {
            var userResult = await _userService.GetUserByEmail(userEmail);
            if (userResult.IsFailed)
            {
                return false;
            }

            if (userResult.Value.IsAdmin)
            {
                return true;
            }

            return await _userResourceRoleRepository.CheckUserHasPermissionForAction(userResult.Value.Id, resourceType, resourceId, resourceAction);
        }

        public async Task<Result<List<ResourceAction>>> GetDepositionUserPermissions(Participant participant, Guid depositionId, bool isAdmin = false)
        {
            if (isAdmin)
            {
                var adminPermissions = Enum.GetValues(typeof(ResourceAction)).Cast<ResourceAction>().ToList(); // return all ResourceAction if IsAdmin
                return Result.Ok(adminPermissions);
            }

            if (participant == null)
                return Result.Fail(new ResourceNotFoundError("Participant can not be null"));

            if (participant.User == null)
                return Result.Fail(new ResourceNotFoundError("Participant User can not be null"));

            var permissions = await _userResourceRoleRepository.GetUserActionsForResource(participant.User.Id, ResourceType.Deposition, depositionId);
            return Result.Ok(permissions);
        }
    }
}
