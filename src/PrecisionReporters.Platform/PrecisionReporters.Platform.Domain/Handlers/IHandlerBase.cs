using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Handlers.Interfaces
{
    public interface IHandlerBase<T>
    {
        void SetSuccessor(IHandlerBase<T> successor);
        Task HandleRequest(T request);
    }
}
