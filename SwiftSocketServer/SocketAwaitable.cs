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
        private readonly static Action SENTINEL = () => { };

        internal bool wasCompleted;
        internal Action continuation;
        readonly SocketAsyncEventArgs eventArgs;

        public SocketAwaitable()
        {
            this.eventArgs = new SocketAsyncEventArgs();
            eventArgs.Completed += delegate
            {
                var prev = continuation ?? Interlocked.CompareExchange(
                    ref continuation, SENTINEL, null);
                if (prev != null) prev();
            };
        }

        public SocketAsyncEventArgs EventArgs
        {
            get
            {
                return eventArgs;
            }
        }

        internal void Reset()
        {
            wasCompleted = false;
            continuation = null;
        }

        public SocketAwaitable GetAwaiter() { return this; }

        public bool IsCompleted { get { return wasCompleted; } }

        public void OnCompleted(Action continuation)
        {
            if (continuation == SENTINEL ||
                Interlocked.CompareExchange(
                    ref continuation, continuation, null) == SENTINEL)
            {
                Task.Run(continuation);
            }
        }

        public void GetResult()
        {
            if (eventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)eventArgs.SocketError);
        }
    }
}
