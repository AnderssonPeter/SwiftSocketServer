using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftSocketServer
{
    
    public sealed class BufferManager
    {
        public sealed class Buffer
        {
            private readonly object locker = new object();
            private readonly int blockSize;
            private readonly byte[] buffer;
            private readonly bool[] freeFragments;

            public Buffer(int blockSize, int blockCountPerBuffer)
            {
                this.blockSize = blockSize;
                this.buffer = new byte[blockCountPerBuffer * blockSize];
                this.freeFragments = new bool[blockCountPerBuffer];
                for(int i = 0; i < blockCountPerBuffer; i++)
                {
                    freeFragments[i] = true;
                }
            }

            public bool TryGetFragment(int minimumSize, out ArraySegment<byte> fragment)
            {
                int blocksNeeded = minimumSize / blockSize;
                if (minimumSize % blockSize != 0)
                    blocksNeeded++;

                lock(locker)
                {
                    int foundInARow = 0;
                    for (int i = 0; i < freeFragments.Length; i++)
                    {
                        if (freeFragments[i])
                            foundInARow++;
                        else
                            foundInARow = 0;

                        if (foundInARow == blocksNeeded)
                        {
                            //Mark the used as used!
                            for(int i2 = i - blocksNeeded + 1; i2 < i + 1; i2++)
                            {
                                freeFragments[i2] = false;
                            }
                            fragment = new ArraySegment<byte>(buffer, (i - blocksNeeded + 1) * blockSize, blockSize * blocksNeeded);
                            return true;
                        }
                    }
                }
                fragment = default(ArraySegment<byte>);
                return false;
            }

            public bool TryPutFragment(ArraySegment<byte> fragment)
            {
                if (fragment.Array != buffer)
                    return false;
                int start = fragment.Offset / blockSize;
                int length = fragment.Count / blockSize;
                lock (locker)
                {
                    for (int i = start; i < start + length; i++)
                    {
                        freeFragments[i] = true;
                    }
                }
                return true;
            }
        }

        private readonly object locker = new object();
        private readonly int blockSize;
        private readonly int blockCountPerBuffer;
        private readonly List<Buffer> buffers = new List<Buffer>();

        public BufferManager(int blockSize, int blockCountPerBuffer)
        {
            this.blockSize = blockSize;
            this.blockCountPerBuffer = blockCountPerBuffer;
            buffers.Add(new Buffer(blockSize, blockCountPerBuffer));
        }

        public ArraySegment<byte> CheckoutFragment(int minimumSize)
        {
            if (minimumSize > blockSize * blockCountPerBuffer)
                throw new ArgumentOutOfRangeException("minimumSize", "to large for buffer");
            if (minimumSize <= 0)
                throw new ArgumentOutOfRangeException("minimumSize", "to large small");
            ArraySegment<byte> value;
            lock(locker)
            {
                foreach (Buffer buffer in buffers)
                {
                    if (buffer.TryGetFragment(minimumSize, out value))
                        return value;
                }

                Buffer newBuffer = new Buffer(blockSize, blockCountPerBuffer);
                newBuffer.TryGetFragment(minimumSize, out value);
                buffers.Add(newBuffer);
            }
            return value;
        }

        public void CheckinFragment(ArraySegment<byte> fragment)
        {
            lock (locker)
            {
                foreach(Buffer buffer in buffers)
                {
                    if (buffer.TryPutFragment(fragment))
                        return;
                }
            }
            throw new ArgumentException("not from this buffer manager", "fragment");
        }
    }
}
