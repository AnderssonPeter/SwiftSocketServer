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
        private readonly Action<T> clearObject;
        private readonly ConcurrentBag<T> bag = new ConcurrentBag<T>();

        public ObjectPool(Func<T> objectGenerator, Action<T> clearObject, int initialCount)
        {
            if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
            this.objectGenerator = objectGenerator;
            this.clearObject = clearObject;
            for (int i = 0; i < initialCount; i++)
                bag.Add(objectGenerator());
        }

        public ObjectPool(Func<T> objectGenerator, Action<T> clearObject)
            : this(objectGenerator, null, 0)
        {
        }

        public ObjectPool(Func<T> objectGenerator) 
            : this(objectGenerator, null)
        { }

        public T GetObject()
        {
            T item;
            if (bag.TryTake(out item))
            {
                if (clearObject != null)
                    clearObject(item);
                return item;
            }
            return objectGenerator();
        }

        public void PutObject(T item)
        {
            bag.Add(item);
        }
    }
}
