using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Threading;

namespace PrecisionReporters.Platform.Transcript.Api.Utils
{
    // Disable rule: It's wrong to use a finalizer without having unmanaged resources to clean. See https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose#implement-the-dispose-pattern
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class ConnectionResources : IDisposable
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        public SemaphoreSlim SemaphoreSlim { get; } = new SemaphoreSlim(1, 1);
        public ServiceScopeContainer<ITranscriptionLiveService> ServiceScopeContainer { get; set; }

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
                SemaphoreSlim.Dispose();
                ServiceScopeContainer.Dispose();
            }

            _disposed = true;
        }
    }
}
