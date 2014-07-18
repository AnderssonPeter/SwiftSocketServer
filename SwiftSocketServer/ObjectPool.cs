using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwiftSocketServer
{
    public sealed class ObjectPool<T>
    {
        private readonly Func<T> objectGenerator;
        private readonly Action<T> onCheckIn;
        private readonly Action<T> onCheckOut;
        private readonly ConcurrentBag<T> bag = new ConcurrentBag<T>();

        public ObjectPool(Func<T> objectGenerator, Action<T> checkIn, Action<T> checkOut, int initialCount)
        {
            if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
            this.objectGenerator = objectGenerator;
            this.onCheckIn = checkIn;
            this.onCheckOut = checkOut;
            for (int i = 0; i < initialCount; i++)
                bag.Add(objectGenerator());
        }

        public ObjectPool(Func<T> objectGenerator, Action<T> checkIn, Action<T> checkOut)
            : this(objectGenerator, checkIn, checkOut, 0)
        {
        }

        public ObjectPool(Func<T> objectGenerator, Action<T> checkIn)
            : this(objectGenerator, checkIn, null, 0)
        {
        }

        public ObjectPool(Func<T> objectGenerator)
            : this(objectGenerator, null)
        { }

        public T Checkout()
        {
            T item;
            if (!bag.TryTake(out item))
            {
                item = objectGenerator();
            }
            if (onCheckOut != null)
                onCheckOut(item);
            return item;
        }

        public void Checkin(T item)
        {
            if (onCheckIn != null)
                onCheckIn(item);
            bag.Add(item);
        }
    }
}
