using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SwiftSocketServer
{
    public sealed class Server
    {
        BufferManager bufferManager;
        ObjectPool<Socket> socketPool;
        ObjectPool<SocketAwaitable> socketAsyncEventArgsPool;
        ObjectPool<Connection> connectionPool;
        Socket listeningSocket;
        Task listeningTask;

        public Server(BufferManager bufferManager)
        {
            this.bufferManager = bufferManager;
            this.socketPool = new ObjectPool<Socket>(() => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), ResetSocket);
            this.socketAsyncEventArgsPool = new ObjectPool<SocketAwaitable>(() => new SocketAwaitable(), ResetSocketAwaitable);
        }

        public void StartListening(int port, int backlog)
        {
            Socket listeningSocket = socketPool.GetObject();
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            listeningSocket.Listen(backlog);

            listeningTask = ListenLoop();
        }
        
        private async Task ListenLoop()
        {
            SocketAwaitable socketAwaitable = socketAsyncEventArgsPool.GetObject();
            socketAwaitable.EventArgs.AcceptSocket = socketPool.GetObject();
            while (true)
            {
                await listeningSocket.AcceptAsync(socketAwaitable);
                Socket socket = socketAwaitable.EventArgs.AcceptSocket;
                if (socket.Connected)
                {
                    SocketConnected(socket);
                    socketAwaitable.EventArgs.AcceptSocket = socketPool.GetObject();
                }
            }
        }

        private void SocketConnected(Socket socket)
        {

        }

        private static void ResetSocket(Socket socket)
        {
            if (socket.Connected)
                socket.Disconnect(true);
        }

        private void ResetSocketAwaitable(SocketAwaitable socketAwaitable)
        {
            if (socketAwaitable.EventArgs.AcceptSocket != null)
                socketPool.PutObject(socketAwaitable.EventArgs.AcceptSocket);
            if (socketAwaitable.EventArgs.BufferList != null)
                socketPool.PutObject(socketAwaitable.EventArgs.AcceptSocket);
        }
    }


    public class Connection 
    {
        internal SocketAwaitable socket;

    }

    public class INetworkStack<TOutput>
    {
        private readonly INetworkLayer[] middleLayers;

        public INetworkLowerLayer<Packet> NetworkServer
        { get; private set; }

        public INetworkLayer<TOutput> LogicServer
        { get; private set; }

        public void Reset()
        {
            foreach (INetworkLayer middleLayer in middleLayers)
                middleLayer.Reset();
        }
    }

    public class BinaryPacket
    {
        ArraySegment<byte> Buffer
        { get; set; }

        public int Count
        { get; set; }
    }

    public class IncommingPacket<TConnection, TPacket> where TConnection : Connection
    {
        public TConnection Connection
        { get; set; }

        public TPacket Packet
        { get; set; }
    }

    public interface INetworkLayer<TConnection> where TConnection : Connection
    {
        Task Close(TConnection connection);
        Task<TConnection> AcceptConnectionAsync();
    }

    public interface INetworkLayer<TConnection, TOutgoing, TIncomming> : INetworkLayer<TConnection>
    {
        Task SendAsync(TOutgoing packet, TConnection connection);
        Task<IncommingPacket<TConnection, TIncomming>> ReciveAsync();
    }
}
