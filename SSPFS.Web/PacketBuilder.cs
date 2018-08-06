using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSPFS.Web
{
    public static class PacketBuilder
    {
        public static byte[] ListFilesForClientPacket()
        {
            return new ClientToServerPacket((int) PacketTypeEnum.ListFiles, 0).GetBytes();
        }
        public static byte[] RequestFileForDownload(string filename)
        {
            var filename_bytes = Encoding.UTF8.GetBytes(filename);

            return
                new ClientToServerPacket((int)PacketTypeEnum.DownloadFile, filename_bytes.Length)
                .GetBytes(filename_bytes);
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

            public byte[] GetBytes(byte[] content = null)
            {
                var bytes = BitConverter.GetBytes(request_code)
                    .Concat(BitConverter.GetBytes(request_length));

                return content == null ? bytes.ToArray() : bytes.Concat(content).ToArray();
            }
        }

    }
}
