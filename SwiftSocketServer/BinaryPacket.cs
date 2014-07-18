using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwiftSocketServer
{
    public class BinaryPacket
    {
        public ArraySegment<byte> Buffer
        { get; set; }

        public int Count
        { get; set; }

        public void Reset(BufferManager bufferManager)
        {
            if (Buffer != null)
            {
                bufferManager.CheckinFragment(Buffer);
                Buffer = default(ArraySegment<byte>);
            }
            Count = 0;
        }
    }
}
