using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwiftSocketServer
{
    public class IncommingPacket<TConnection, TPacket> where TConnection : Connection
    {
        public TConnection Connection
        { get; set; }

        public TPacket Packet
        { get; set; }
    }
}
