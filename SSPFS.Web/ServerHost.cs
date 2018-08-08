using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SSPFS
{
    public class ServerHost
    {
        public static ServerHost Current { get; set; }
        public delegate void ProcessClientResponseDelegateHandler(Stream client_response, long response_length);

        IPEndPoint listening_endpoint;
        Task host_update_daemon;
        public ConcurrentDictionary<Guid, HostClient> Hosts = new ConcurrentDictionary<Guid, HostClient>();

        public ServerHost(IPEndPoint listening_endpoint)
        {
            this.listening_endpoint = listening_endpoint;
        }

        public void Start()
        {
            TcpListener listener = new TcpListener(listening_endpoint);
            listener.Start();
            listener.BeginAcceptSocket(new AsyncCallback(AcceptClient), listener);

            //iniciamos un hilo que va a solicitar la actualizacion de los servidores 
            //de forma ordenada.
            host_update_daemon = Task.Run(() =>
            {
                while (true)
                {
                    foreach (var host in Hosts)
                    {
                        try
                        {
                            //si el host se ha desconectado, lo borramos de la lista y continuamos con el siguiente
                            if (!host.Value.IsConnected)
                            {
                                Hosts.TryRemove(host.Key, out HostClient h);
                                continue;
                            }

                            host.Value.ProcessPendingMessages();
                        }
                        catch (Exception ex)
                        {
                            //todo: tratar los errores del hilo de actualización
                        }
                    }
                    Thread.Sleep(10);
                }
            });
        }

        private void AcceptClient(IAsyncResult ar)
        {
            var listener = (ar.AsyncState as TcpListener);
            var new_client = listener.EndAcceptTcpClient(ar);
            listener.BeginAcceptSocket(new AsyncCallback(AcceptClient), listener);

            var client = HostClient.ForClient(new_client);
            if (!Hosts.TryAdd(client.Identifier, client))
                client.Disconnect();
        }

    }
}
