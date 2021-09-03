using PrecisionReporters.Platform.Domain.Handlers.Interfaces;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Handlers
{
    /// <summary>
    /// The 'Handler' abstract class
    /// </summary>
    public abstract class HandlerBase<T> : IHandlerBase<T>
    {
        protected IHandlerBase<T> successor;

        public HandlerBase()
        {
        }

        public void SetSuccessor(IHandlerBase<T> successor)
        {
            this.successor = successor;
        }
        public abstract Task HandleRequest(T request);
    }
}
