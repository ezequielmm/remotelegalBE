using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class DepositionService : IDepositionService
    {
        private readonly IDepositionRepository _depositionRepository;
        private readonly IUserService _userService;


        public DepositionService(IDepositionRepository depositionRepository, IUserService userService)
        {
            _depositionRepository = depositionRepository;
            _userService = userService;
        }

        public async Task<List<Deposition>> GetDepositions(Expression<Func<Deposition, bool>> filter = null,
            string[] include = null)
        {
            return await _depositionRepository.GetByFilter(filter, include);
        }

        public async Task<Result<Deposition>> GetDepositionById(Guid id)
        {
            var deposition = await _depositionRepository.GetById(id, new[] { nameof(Deposition.Documents) });
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            return Result.Ok(deposition);
        }

        public async Task<Result<Deposition>> GenerateScheduledDeposition(Deposition deposition, List<DepositionDocument> uploadedDocuments)
        {
            var requester = await _userService.GetUserByEmail(deposition.Requester.EmailAddress);
            if (requester == null)
            {
                return Result.Fail(new ResourceNotFoundError($"Requester with email {deposition.Requester.EmailAddress} not found"));
            }
            deposition.Requester = requester;

            if (deposition.Witness != null)
            {
                if (!string.IsNullOrWhiteSpace(deposition.Witness.Email))
                {
                    var witnessUser = await _userService.GetUserByEmail(deposition.Witness.Email);
                    if (witnessUser != null)
                    {
                        deposition.Witness.User = witnessUser;
                    }
                }
            }

            // If caption has a FileKey, find the matching document. If it doesn't has a FileKey, remove caption
            deposition.Caption = !string.IsNullOrWhiteSpace(deposition.FileKey) ? uploadedDocuments.First(d => d.FileKey == deposition.FileKey) : null;

            return Result.Ok(deposition);
        }
    }
}
