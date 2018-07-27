using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SSPFS.Web
{
    public class ServerHost
    {
        IPEndPoint listening_endpoint;
        public ServerHost(IPEndPoint listening_endpoint)
        {
            this.listening_endpoint = listening_endpoint;
        }

        public void StartListening()
        {
            TcpListener listener = new TcpListener(listening_endpoint);
            listener.Start();
            listener.BeginAcceptSocket(new AsyncCallback(AcceptClient), listener);
        }
        private void AcceptClient(IAsyncResult ar)
        {
            var listener = (ar.AsyncState as TcpListener);
            var new_client = listener.EndAcceptTcpClient(ar);
            
            listener.BeginAcceptSocket(new AsyncCallback(AcceptClient), listener);
        }
    }
}
