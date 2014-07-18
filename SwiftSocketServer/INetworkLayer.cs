using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwiftSocketServer
{
    public interface INetworkLayer<TConnection, TOutgoing, TIncomming> where TConnection : Connection
    {
        Task Close(TConnection connection);
        Task<TConnection> AcceptConnectionAsync();
        Task SendAsync(TOutgoing packet, TConnection connection);
        Task<IncommingPacket<TConnection, TIncomming>> ReciveAsync();
    }
}
