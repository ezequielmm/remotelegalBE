using FluentResults;
using System.Dynamic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ISystemSettingsService
    {
        Task<Result<ExpandoObject>> GetAll();
    }
}
