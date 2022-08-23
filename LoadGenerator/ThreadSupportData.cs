using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LoadGenerator
{
    public class ThreadSupportData
    {
        private bool isDisposed = false;
        public ThreadSupportData(CancellationTokenSource source)
        {
            Source = source;
            TokenHash = Token.GetHashCode();
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            Source.Dispose();
        }

        /// <summary>
        /// Tasks are only set when using dynamic load test.
        /// </summary>
        public System.Threading.Tasks.Task Task { get; set; }
        public CancellationTokenSource Source { get; set; }
        public CancellationToken Token { get { return Source.Token; } }
        public int TokenHash { get; set; }
    }
}
