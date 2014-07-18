using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SwiftSocketServer
{

    public class Connection
    {
        private BufferManager bufferManager;
        private ObjectPool<BinaryPacket> binaryPacketPool;
        private int packetSize;

        private readonly Dictionary<object, object> layerData = new Dictionary<object, object>();

        private readonly object readLocker = new object();
        private readonly SocketAwaitable readHandler = new SocketAwaitable();
        private readonly BufferBlock<BinaryPacket> receivedPackets = new BufferBlock<BinaryPacket>();
        private Task readTask;

        private readonly object writeLocker = new object();
        private readonly SocketAwaitable writeHandler = new SocketAwaitable();
        private readonly BufferBlock<BinaryPacket> queuedWritePackets = new BufferBlock<BinaryPacket>();
        private Task writeTask;

        private Socket socket;
        
        internal Socket Socket
        { 
            get { return socket; }
            set { socket = value; }
        }
        
        internal void Initialize(BufferManager bufferManager, ObjectPool<BinaryPacket> binaryPacketPool, int packetSize)
        {
            this.binaryPacketPool = binaryPacketPool;
            this.bufferManager = bufferManager;
            this.packetSize = packetSize;
        }

        internal void Setup()
        {
            SetReadBuffer(bufferManager.CheckoutFragment(packetSize));
            SetWriteBuffer(bufferManager.CheckoutFragment(packetSize));
        }

        public T GetData<T>(object key) where T : class
        {
            lock (layerData)
            {
                return (T)layerData[key];
            }
        }

        public void SetData<T>(object key, T value) where T : class
        {
            lock (layerData)
            {
                layerData[key] = value;
            }
        }

        public void RemoveData(object key)
        {
            lock (layerData)
            {
                layerData.Remove(key);
            }
        }
        
        private void SetReadBuffer(ArraySegment<byte> buffer)
        {
            lock (readLocker)
            {
                IList<ArraySegment<byte>> bufferList = readHandler.EventArgs.BufferList;
                if (bufferList == null)
                    bufferList = new List<ArraySegment<byte>>(1);
                if (bufferList.Count > 0)
                    throw new InvalidOperationException("Already has buffer");
                bufferList.Add(buffer);
            }
        }

        private bool TryRemoveReadBuffer(out ArraySegment<byte> buffer)
        {
            lock (readLocker)
            {
                IList<ArraySegment<byte>> bufferList = readHandler.EventArgs.BufferList;
                if (bufferList == null)
                {
                    buffer = default(ArraySegment<byte>);
                    return false;
                }
                buffer = bufferList[0];
                bufferList.Clear();
                return true;
            }
        }

        private void SetWriteBuffer(ArraySegment<byte> buffer)
        {
            lock (writeLocker)
            {
                IList<ArraySegment<byte>> bufferList = writeHandler.EventArgs.BufferList;
                if (bufferList == null)
                    bufferList = new List<ArraySegment<byte>>(1);
                if (bufferList.Count > 0)
                    throw new InvalidOperationException("Already has buffer");
                bufferList.Add(buffer);
            }
        }

        private bool TryRemoveWriteBuffer(out ArraySegment<byte> buffer)
        {
            lock (writeLocker)
            {
                IList<ArraySegment<byte>> bufferList = writeHandler.EventArgs.BufferList;
                if (bufferList == null)
                {
                    buffer = default(ArraySegment<byte>);
                    return false;
                }
                buffer = bufferList[0];
                bufferList.Clear();
                return true;
            }
        }

        internal void Reset()
        {
            layerData.Clear();
            IList<BinaryPacket> packets;
            if (queuedWritePackets.TryReceiveAll(out packets))
                foreach(BinaryPacket packet in packets)
                    binaryPacketPool.Checkin(packet);

            if (receivedPackets.TryReceiveAll(out packets))
                foreach (BinaryPacket packet in packets)
                    binaryPacketPool.Checkin(packet);

            ArraySegment<byte> buffer;
            if (TryRemoveReadBuffer(out buffer))
                bufferManager.CheckinFragment(buffer);
            if (TryRemoveWriteBuffer(out buffer))
                bufferManager.CheckinFragment(buffer);
        }

        public void StartReading()
        {
            lock(readLocker)
            {
                readTask = Task.Run(async () =>
                {
                    while(true)
                    {
                        await socket.ReceiveAsync(readHandler);
                        if (readHandler.EventArgs.BytesTransferred == 0)
                            break;
                        BinaryPacket packet = binaryPacketPool.Checkout();
                        packet.Count = readHandler.EventArgs.BytesTransferred;
                        packet.Buffer = bufferManager.CheckoutFragment(packet.Count);
                        for (int i = 0; i < packet.Count; i++)
                        {
                            packet.Buffer.Array[packet.Buffer.Offset + i] = readHandler.EventArgs.BufferList[0].Array[readHandler.EventArgs.BufferList[0].Offset + i];
                        }
                        if (!receivedPackets.Post(packet))
                            break;
                    }
                });
            }
        }

        public async Task<BinaryPacket> ReceiveAsync()
        {
            return await receivedPackets.ReceiveAsync();
        }

        internal void StartWriting()
        {
            lock (readLocker)
            {
                readTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        await socket.ReceiveAsync(readHandler);
                        //do some thing
                    }
                });
            }
        }

        public Connection()
        {
            readHandler = new SocketAwaitable();
            writeHandler = new SocketAwaitable();
        }

        
    }
}
