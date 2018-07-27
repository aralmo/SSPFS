using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SSPFS.Web
{
    public class HostClient
    {
        public Guid Identifier { get; } = new Guid();
        internal TcpClient client;

        public HostClient(TcpClient client)
        {            
        }
    }
}
