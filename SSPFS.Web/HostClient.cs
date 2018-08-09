using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SSPFS
{
    public class HostClient
    {
        //variables que indican en que estado está la lectura de una respuesta del cliente.
        internal HostClientStatusEnum CurrentStatus = HostClientStatusEnum.Listening;
        private Guid current_response_identifier = Guid.Empty;
        private long current_response_length = 0;

        private object request_queue_lock = new object();
        private List<Request> requests_queue = new List<Request>();
        internal object requests_queue_lock = new object();

        internal DateTime last_keep_alive = DateTime.Now;

        public Guid Identifier { get; } = Guid.NewGuid();
        internal TcpClient tcp_client;

        public static string HostAddress { get;set; } = "http://localhost";

        /// <summary>
        /// Indica si el cliente está conectado y funcionando.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return tcp_client.Connected;
            }
        }

        private HostClient() { }
        public static HostClient ForClient(TcpClient client)
        {
            HostClient new_client = new HostClient();
            new_client.tcp_client = client;
            return new_client;
        }
        public void ProcessPendingMessages()
        {

            //Si el cliente perdió la conexión lanzamos el evento y petamos como dios.
            if (!tcp_client.Connected)
            {
                ServerAPI.Current.ReportRemoteHostDisconnect(Identifier);
                ServerHost.Current.Hosts.TryRemove(Identifier, out _);
                return;
            }

            var stream = tcp_client.GetStream();

            //Estamos escuchando al cliente?
            if (CurrentStatus == HostClientStatusEnum.Listening)
            {
                byte[] cabecera = new byte[24];
                int readen = stream.Read(cabecera, 0, 24);
                if (readen != 24)
                    throw new Exception("Error de tamaño al leer la cabecera");

                //establecemos las variables de lectura y el estado del cliente
                current_response_identifier = new Guid(cabecera.Take(16).ToArray());

                if (current_response_identifier == Guid.Empty)
                {
                    long client_message_code = BitConverter.ToInt64(cabecera, 16);
                    ProcessClientMessage(client_message_code);
                }
                else
                {
                    current_response_length = BitConverter.ToInt64(cabecera, 16);
                    CurrentStatus = HostClientStatusEnum.RecibiendoRespuesta;
                }
            }
            else
            {
                if (CurrentStatus == HostClientStatusEnum.RecibiendoRespuesta)
                {
                    //descargar contenido de respuesta.
                    Request request;
                    lock (request_queue_lock)
                    {
                        request = requests_queue.FirstOrDefault(x => x.RequestIdentifier == current_response_identifier);
                    }

                    if (request != null && request.Callback != null)
                    {
                        //desencolamos la request                        
                        lock (request_queue_lock)
                        {
                            requests_queue.Remove(request);
                        }

                        //delegamos el procesamiento de la respuesta a la callback
                        request.Callback(stream, current_response_length);
                    }
                    else
                    {
                        throw new Exception("No se pudo procesar la solicitud, por no existir.");
                    }
                }
            }
        }

        /// <summary>
        /// Procesa los mensajes de cliente que no hacen referencia a una solicitud.
        /// </summary>
        /// <param name="message_code"></param>
        private void ProcessClientMessage(long message_code)
        {
            lock (requests_queue_lock)
            {
                if (message_code == 1)
                {
                    ServerAPI.Current.ReportRemoteHostChanged(Identifier);
                }
                if (message_code == 2)
                {
                    //keep alive.
                    last_keep_alive = DateTime.Now;
                }
                if (message_code == 3)
                {
                    //solicitud de URL de acceso externa
                    string url =  $"{HostAddress}/docbox/index/{Identifier.ToString()}";
                    byte[] url_bytes = Encoding.UTF8.GetBytes(url);
                    int size = url_bytes.Length;
                    var pck = BitConverter.GetBytes(size).Concat(url_bytes).ToArray();
                    tcp_client.GetStream().Write(pck, 0, pck.Length);
                }
            }
        }

        public void EnqueueRequest(byte[] data, ServerHost.ProcessClientResponseDelegateHandler callback)
        {
            var request = new Request()
            {
                RequestData = data,
                Callback = callback
            };

            lock (requests_queue_lock)
            {
                requests_queue.Add(request);
            }


            var request_bytes = request.RequestIdentifier
                .ToByteArray()
                .Concat(data)
                .ToArray();

            lock (requests_queue_lock)
            {
                tcp_client.GetStream().Write(request_bytes, 0, request_bytes.Length);
            }
        }

        internal void Disconnect()
        {
            //Cerramos la conexión TCP
            tcp_client.Close();
        }

        /// <summary>
        /// Clase para encolar las solicitudes realizadas a un cliente.
        /// </summary>
        private class Request
        {
            public Guid RequestIdentifier { get; } = Guid.NewGuid();
            public byte[] RequestData { get; set; }
            public ServerHost.ProcessClientResponseDelegateHandler Callback { get; set; }
        }
    }
}
