using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SwiftSocketServer.Test
{
    [TestClass]
    public class BufferTests
    {
        [TestMethod]
        public void GetSameSizeTest()
        {
            const int blockSize = 1024;
            const int blockCount = 100;
            ArraySegment<byte> fragment;
            var buffer = new BufferManager.Buffer(blockSize, blockCount);
            for (int i = 0; i < blockCount; i++)
            {
                Assert.IsTrue(buffer.TryGetFragment(blockSize, out fragment), "Failed to get fragment!");
            }
            Assert.IsFalse(buffer.TryGetFragment(blockSize, out fragment), "Should fail to get fragment!");
        }

        [TestMethod]
        public void GetDiffrentSizeTest()
        {
            const int blockSize = 1024;
            const int blockCount = 10;
            ArraySegment<byte>[] fragments = new ArraySegment<byte>[3];
            var buffer = new BufferManager.Buffer(blockSize, blockCount);

            Assert.IsTrue(buffer.TryGetFragment(blockSize, out fragments[0]), "Failed to get fragment!");
            Assert.IsTrue(buffer.TryGetFragment(blockSize * 2, out fragments[1]), "Failed to get fragment!");
            Assert.IsTrue(buffer.TryGetFragment(blockSize * 3, out fragments[2]), "Failed to get fragment!");
            Assert.AreEqual(0, fragments[0].Offset / blockSize);
            Assert.AreEqual(1, fragments[1].Offset / blockSize);
            Assert.AreEqual(3, fragments[2].Offset / blockSize);

        }

        [TestMethod]
        public void PutTest()
        {
            const int blockSize = 1024;
            const int blockCount = 10;
            ArraySegment<byte>[] fragments = new ArraySegment<byte>[3];
            var buffer = new BufferManager.Buffer(blockSize, blockCount);

            Assert.IsTrue(buffer.TryGetFragment(blockSize, out fragments[0]), "Failed to get fragment!");
            Assert.IsTrue(buffer.TryGetFragment(blockSize * 2, out fragments[1]), "Failed to get fragment!");
            Assert.IsTrue(buffer.TryGetFragment(blockSize * 3, out fragments[2]), "Failed to get fragment!");

            buffer.TryPutFragment(fragments[1]);

            Assert.IsTrue(buffer.TryGetFragment(blockSize * 2 + 1, out fragments[1]), "Failed to get fragment!");
            Assert.AreEqual(6, fragments[1].Offset / blockSize);
        }
    }
}
