using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftSocketServer.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            AcceptConnections3().Wait();
            Console.ReadKey();
        }

        static async Task AcceptConnections()
        {
            Socket listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listeningSocket.Blocking = false;
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, 8080));
            listeningSocket.Listen(16);
            SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
            eventArgs.Completed += eventArgs_Completed;
            if (!listeningSocket.AcceptAsync(eventArgs))
            {
                Console.WriteLine("Accepted socket");
            }
        }

        static void eventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine("Accepted socket");
        }

        static async Task AcceptConnections2()
        {
            Console.WriteLine("Waiting for connection");
            Socket listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listeningSocket.Blocking = false;
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, 8080));
            listeningSocket.Listen(16);
            SocketAwaitable awaitableAcceptedSocket = new SocketAwaitable();
            Socket acceptedSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            awaitableAcceptedSocket.EventArgs.AcceptSocket = acceptedSocket;
            await listeningSocket.AcceptAsync(awaitableAcceptedSocket);
            Console.WriteLine("Accepted socket");
            if (object.ReferenceEquals(acceptedSocket, awaitableAcceptedSocket.EventArgs.AcceptSocket))
                Console.WriteLine("Socket was reused");

            Console.WriteLine("Reading data");

            byte[] buffer = new byte[1024];
            ArraySegment<byte> bufferSegement = new ArraySegment<byte>(buffer);
            SocketAwaitable awaitableReadData = new SocketAwaitable();
            awaitableReadData.EventArgs.BufferList = new List<ArraySegment<byte>>(new[] { bufferSegement });
            while (true)
            {
                await acceptedSocket.ReceiveAsync(awaitableReadData);
                string data = Encoding.UTF8.GetString(bufferSegement.Array, bufferSegement.Offset, bufferSegement.Offset + awaitableReadData.EventArgs.BytesTransferred);
                Console.WriteLine(data);
            }
            Console.WriteLine("Data recived");
        }

        static async Task AcceptConnections3()
        {
            BufferManager bufferManager = new BufferManager(1024 * 16, 32);
            ObjectPool<BinaryPacket> binaryPacketPool = new ObjectPool<BinaryPacket>(() => new BinaryPacket(), (bp) => bp.Reset(bufferManager));
            SocketServer<Connection> server = new SocketServer<Connection>(bufferManager, binaryPacketPool, 1024);
            server.StartListening(8080, 16);
            Connection connection = await server.AcceptConnectionAsync();
            Console.WriteLine("Accepted socket");
            connection.StartReading();
            Console.WriteLine("Starting to read data!");
            while(true)
            {
                BinaryPacket packet = await connection.ReceiveAsync();
                string message = Encoding.UTF8.GetString(packet.Buffer.Array, packet.Buffer.Offset, packet.Buffer.Offset + packet.Count);
                binaryPacketPool.Checkin(packet);
                Console.WriteLine(message);
            }
        }
    }

  
}
