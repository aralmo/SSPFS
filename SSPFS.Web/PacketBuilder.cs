using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SSPFS.Web
{
    public static class PacketBuilder
    {
        public static byte[] ListFilesForClientPacket()
        {
            return new ClientToServerPacket(1, 0).GetBytes();
        }

        class ClientToServerPacket
        {
            private int request_code;
            private long request_length;

            public ClientToServerPacket(int request_code, long request_length)
            {
                this.request_code = request_code;
                this.request_length = request_length;
            }

            public byte[] GetBytes()
            {
                return BitConverter.GetBytes(request_code)
                    .Concat(BitConverter.GetBytes(request_length)).ToArray();
            }
        }
    }
}
