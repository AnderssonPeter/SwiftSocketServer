using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SwiftSocketServer.Test
{
    [TestClass]
    public class BufferManagerTests
    {

        [TestMethod]
        public void GetSameSizeTest()
        {
            const int blockSize = 1024;
            const int blockCount = 100;
            ArraySegment<byte> fragment;
            var buffer = new BufferManager(blockSize, blockCount);
            for (int i = 0; i < blockCount * 3; i++)
            {
                fragment = buffer.GetFragment(blockSize * (i % 3) + 1);
            }
        }

        [TestMethod]
        public void GetDiffrentSizeTest()
        {
            const int blockSize = 1024;
            const int blockCount = 10;
            ArraySegment<byte>[] fragments = new ArraySegment<byte>[3];
            var buffer = new BufferManager(blockSize, blockCount);

            fragments[0] = buffer.GetFragment(blockSize);
            fragments[1] = buffer.GetFragment(blockSize * 2);
            fragments[2] = buffer.GetFragment(blockSize * 3);

        }

        [TestMethod]
        public void PutTest()
        {
            const int blockSize = 1024;
            const int blockCount = 10;
            ArraySegment<byte>[] fragments = new ArraySegment<byte>[3];
            var buffer = new BufferManager(blockSize, blockCount);

            fragments[0] = buffer.GetFragment(blockSize);
            fragments[1] = buffer.GetFragment(blockSize * 2);
            fragments[2] = buffer.GetFragment(blockSize * 3);

            buffer.PutFragment(fragments[1]);

            fragments[1] = buffer.GetFragment(blockSize * 2 + 1);
        }
    }
}
