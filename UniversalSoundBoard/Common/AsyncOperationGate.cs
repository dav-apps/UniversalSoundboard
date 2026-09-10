using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalSoundboard.Common
{
    // Keep async operations on one audio graph serialized, including after a failed operation.
    internal sealed class AsyncOperationGate
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public async Task RunAsync(Func<Task> operation)
        {
            await semaphore.WaitAsync();
            try { await operation(); }
            finally { semaphore.Release(); }
        }
    }
}
