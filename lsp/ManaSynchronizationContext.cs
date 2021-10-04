namespace vein.lsp
{
    using System.Threading;
    using Microsoft.VisualStudio.Threading;
    using Xunit;

    /// <summary>
    /// Used to enforce in-order processing of the communication with the Q# language server.
    /// Such a synchronization context is needed since the Q# language server
    /// processes changes incrementally rather than reprocessing entire files each time.
    /// </summary>
    public class ManaSynchronizationContext : SynchronizationContext
    {
        private readonly AsyncQueue<(SendOrPostCallback, object?)> queued = new();

        private void ProcessNext()
        {
            var gotNext = this.queued.TryDequeue(out var next);
            Assert.True(gotNext, "nothing to process in the SynchronizationContext");
            if (gotNext)
            {
                next.Item1(next.Item2);
            }
        }

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback fct, object? arg)
        {
            this.queued.Enqueue((fct, arg));
            this.Send(_ => this.ProcessNext(), null);
        }
    }
}
