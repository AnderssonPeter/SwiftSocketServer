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
    }

    public interface IServerInterface<ConnectionType> where ConnectionType : Connection, new()
    {
        Task NewConnection(ConnectionType connection);
    }
}
