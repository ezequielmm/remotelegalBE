using FluentResults;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Shared.Helpers.Interfaces
{
    public interface ISnsHelper
    {
        Task<Result> SubscribeEndpoint(string subscribeURL);
    }
}
