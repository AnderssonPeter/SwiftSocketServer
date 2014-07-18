using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SwiftSocketServer
{
    public sealed class SocketServer<TConnection> : INetworkLayer<TConnection, BinaryPacket, BinaryPacket> where TConnection : Connection, new()
    {
        readonly object locker = new object();
        BufferManager bufferManager;
        ObjectPool<Socket> socketPool;
        ObjectPool<TConnection> connectionPool;
        ObjectPool<BinaryPacket> binaryPacketPool;
        ObjectPool<SocketAwaitable> listeningHandlerPool = new ObjectPool<SocketAwaitable>(() => new SocketAwaitable());
        Socket listeningSocket;

        public SocketServer(BufferManager bufferManager, ObjectPool<BinaryPacket> binaryPacketPool, int packetSize)
        {
            this.bufferManager = bufferManager;
            this.binaryPacketPool = binaryPacketPool;
            this.socketPool = new ObjectPool<Socket>(() => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), ResetSocket);
            this.binaryPacketPool = new ObjectPool<BinaryPacket>(() => new BinaryPacket(), (bp) => bp.Reset(bufferManager));
            this.connectionPool = new ObjectPool<TConnection>(() => { TConnection connection = new TConnection(); connection.Initialize(bufferManager, binaryPacketPool, packetSize); return connection; }, ResetConnection, (c) => c.Setup());
        }

        public void StartListening(int port, int backlog)
        {
            lock (locker)
            {
                listeningSocket = socketPool.Checkout();
                listeningSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                listeningSocket.Listen(backlog);
            }
        }

        public void StopListening()
        {
            lock (locker)
            {
                listeningSocket.Shutdown(SocketShutdown.Both);
                listeningSocket.Disconnect(true);
                socketPool.Checkin(listeningSocket);
                listeningSocket = null;
            }
        }

        public async Task<TConnection> AcceptConnectionAsync()
        {
            SocketAwaitable listeningHandler = listeningHandlerPool.Checkout();
            listeningHandler.EventArgs.AcceptSocket = socketPool.Checkout();
            try
            {
                await listeningSocket.AcceptAsync(listeningHandler);
                Socket acceptedSocket = listeningHandler.EventArgs.AcceptSocket;
                TConnection connection = connectionPool.Checkout();
                connection.Socket = acceptedSocket;
                return connection;
            }
            finally
            {
                listeningHandler.EventArgs.AcceptSocket = null;
                listeningHandlerPool.Checkin(listeningHandler);
            }
        }

        private static void ResetSocket(Socket socket)
        {
            if (socket.Connected)
                socket.Disconnect(true);
        }

        private void ResetConnection(TConnection connection)
        {
            if (connection.Socket != null)
            {
                socketPool.Checkin(connection.Socket);
                connection.Socket = null;
            }
            connection.Reset();
        }

        private void ResetSocketAwaitable(SocketAwaitable socketAwaitable)
        {
            if (socketAwaitable.EventArgs.AcceptSocket != null)
                socketPool.Checkin(socketAwaitable.EventArgs.AcceptSocket);
            if (socketAwaitable.EventArgs.BufferList != null)
                socketPool.Checkin(socketAwaitable.EventArgs.AcceptSocket);
        }

        public Task Close(TConnection connection)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(BinaryPacket packet, TConnection connection)
        {
            throw new NotImplementedException();
        }

        public Task<IncommingPacket<TConnection, BinaryPacket>> ReciveAsync()
        {
            throw new NotImplementedException();
        }
    }


}
