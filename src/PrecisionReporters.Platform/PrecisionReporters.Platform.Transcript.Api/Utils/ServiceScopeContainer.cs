using Microsoft.Extensions.DependencyInjection;
using System;

namespace PrecisionReporters.Platform.Transcript.Api.Utils
{
    // Disable rule: It's wrong to use a finalizer without having unmanaged resources to clean. See https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose#implement-the-dispose-pattern
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class ServiceScopeContainer<T> : IDisposable
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        public IServiceScope ServiceScope { get; set; }
        public T Service { get; set; }

        private bool _disposed = false;

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                ServiceScope.Dispose();
            }

            _disposed = true;
        }
    }
}
