using FluentResults;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly ISystemSettingsRepository _repository;
        public SystemSettingsService(ISystemSettingsRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<ExpandoObject>> GetAll()
        {
            var data = await _repository.GetByFilter();

            dynamic result = new ExpandoObject();

            IDictionary<string, object> dictionary = (IDictionary<string, object>)result;
            foreach (var item in data)
            {
                dictionary.Add(Enum.GetName(typeof(SystemSettingsName), item.Name), item.Value);
            }
            return Result.Ok(result);
        }
    }
}
