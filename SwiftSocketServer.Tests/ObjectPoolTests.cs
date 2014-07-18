using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace SwiftSocketServer.Test
{
    [TestClass]
    public class ObjectPoolTests
    {
        [TestMethod]
        public void GetObjectTest()
        {
            const int count = 10;
            ObjectPool<TestItem> objectPool = new ObjectPool<TestItem>(() => new TestItem(), (t) => t.CheckOutTime = DateTime.Now, (t) => t.CheckOutTime = null, count / 2);
            TestItem[] ti = new TestItem[count];
            for(int i = 0; i < count; i++)
            {
                ti[i] = objectPool.Checkout();
                Assert.IsNotNull(ti[i]);
            }
        }

        [TestMethod]
        public void PutObjectTest()
        {
            const int count = 10;
            ObjectPool<TestItem> objectPool = new ObjectPool<TestItem>(() => new TestItem(), (t) => t.CheckOutTime = DateTime.Now, (t) => t.CheckOutTime = null, count / 2);
            TestItem[] ti = new TestItem[count];
            for (int i = 0; i < count; i++)
            {
                ti[i] = objectPool.Checkout();
                Assert.IsNotNull(ti[i]);
            }
            for (int i = 0; i < count; i++)
            {
                objectPool.Checkin(ti[i]);
                ti[i] = null;
            }
        }

        class TestItem
        {
            public static int counter;
            public readonly int Id = Interlocked.Increment(ref counter);
            public DateTime? CheckOutTime;

            public TestItem()
            {
                CheckOutTime = DateTime.Now;
            }
        }
    }
}
