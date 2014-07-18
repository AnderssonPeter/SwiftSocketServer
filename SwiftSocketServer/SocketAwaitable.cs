using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftSocketServer
{
    public sealed class SocketAwaitable : INotifyCompletion
    {
        private readonly static Action Sentinel = () => { };

        public bool WasCompleted;
        public Action Continuation;
        public readonly SocketAsyncEventArgs EventArgs;

        public SocketAwaitable()
        {
            EventArgs = new SocketAsyncEventArgs();
            EventArgs.Completed += delegate
            {
                var prev = Continuation ?? Interlocked.CompareExchange(
                    ref Continuation, Sentinel, null);
                if (prev != null) prev();
            };
        }

        internal void Reset()
        {
            WasCompleted = false;
            Continuation = null;
        }

        public SocketAwaitable GetAwaiter() { return this; }

        public bool IsCompleted { get { return WasCompleted; } }

        public void OnCompleted(Action continuation)
        {
            if (this.Continuation == Sentinel ||
                Interlocked.CompareExchange(
                    ref this.Continuation, continuation, null) == Sentinel)
            {
                Task.Run(continuation);
            }
        }

        public void GetResult()
        {
            if (EventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)EventArgs.SocketError);
        }
    }
}
