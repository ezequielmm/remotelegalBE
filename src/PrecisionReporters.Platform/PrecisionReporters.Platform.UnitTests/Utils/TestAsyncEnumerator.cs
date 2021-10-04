using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Run(() => _inner.Dispose());
        }

        public T Current
        {
            get
            {
                return _inner.Current;
            }
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            return await Task.FromResult(_inner.MoveNext());
        }
    }
}
