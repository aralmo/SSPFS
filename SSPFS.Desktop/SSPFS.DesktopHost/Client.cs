using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SSPFS.DesktopHost
{
    public class Client
    {
        public static Client Current;

        string folder;
        public static object host_data_connection_lock = new object();
        const string Host = "127.0.0.1";
        const int Port = 1666;

        Task Connection_daemon;
        TcpClient client = new TcpClient();

        int retry = 5;

        public Client(string folder)
        {
            if (!Directory.Exists(folder))
                throw new DirectoryNotFoundException();

            this.folder = folder;

            Connection_daemon = Task.Run(() =>
            {
                while (true)
                {
                    if (CheckConnection())
                    {
                        retry = 20;
                        ProcessIncomingRequests();
                        Program.Form.Invoke(new MethodInvoker(() =>
                        {
                            Program.Form.lbEstado.Text = "Conectado";
                            Program.Form.lbEstado.ForeColor = Color.Green;
                        }));
                    }
                    else
                    {
                        Program.Form.Invoke(new MethodInvoker(() =>
                        {
                            Program.Form.lbEstado.Text = "Reconectando...";
                            Program.Form.lbEstado.ForeColor = Color.Red;
                        }));

                        if (--retry == 0)
                        {
                            Program.Form.Invoke(new MethodInvoker(() =>
                            {
                                Program.Form.lbEstado.Text = "Desconectado";
                                Program.Form.lbEstado.ForeColor = Color.Red;
                            }));

                            Log.LogError("No se pudo reconectar al servidor remoto");
                            throw new TimeoutException("No se pudo conectar con el servidor remoto");
                        }
                    }


                    Thread.Sleep(500);
                }
            });
        }

        private void ProcessIncomingRequests()
        {
            var stream = client.GetStream();

            Guid request_id;
            int code;
            long length;
            byte[] data;

            byte[] bguid = new byte[16];
            byte[] bcode = new byte[4];
            byte[] blength = new byte[8];
            byte[] bdata;

            if (stream.DataAvailable)
            {
                //leemos el guid con el código de request
                stream.Read(bguid, 0, 16);
                request_id = new Guid(bguid);

                stream.Read(bcode, 0, 4);
                code = BitConverter.ToInt32(bcode, 0);

                stream.Read(blength, 0, 8);
                length = BitConverter.ToInt64(blength, 0);

                ProcessIncomingPacket(request_id, code, length, stream);
            }
        }

        DateTime last_keepalive_sent = DateTime.MinValue;
        bool CheckConnection()
        {
            try
            {
                if (!client.Connected)
                {
                    //intentamos conectar el cliente
                    client.Connect(new IPEndPoint(IPAddress.Parse(Host), Port));
                    send_keepalive();
                    Log.LogMessage("Conectado");
                    return client.Connected;
                }
                else
                {
                    if ((DateTime.Now - last_keepalive_sent).TotalSeconds > 2)
                    {
                        lock (host_data_connection_lock)
                        {
                            send_keepalive();
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"No se pudo conectar con el servidor remoto -> {ex.Message}");
                return false;
            }
        }

        private void send_keepalive()
        {
            long command = 2;
            byte[] packet_bytes = new byte[16].Concat(BitConverter.GetBytes(command)).ToArray();
            client.GetStream().Write(packet_bytes, 0, packet_bytes.Length);
        }

        private void ProcessIncomingPacket(Guid request, int code, long length, NetworkStream stream)
        {
            //listar ficheros
            if (code == 1)
            {
                //listar fichero.
                if (length > 0)
                    throw new Exception("Bad code 0");

                //montamos el paquete respuesta
                var files = string.Join("|", Directory.GetFiles(folder).Select(x => Path.GetFileName(x)));
                byte[] response_bytes = Encoding.UTF8.GetBytes(files);

                //ignoramos el data de recepción, por ser una peticion de listado.
                var response_length = Convert.ToInt64(response_bytes.Length);
                lock (host_data_connection_lock)
                {
                    stream.Write(request.ToByteArray().Concat(BitConverter.GetBytes(response_length)).ToArray(), 0, 24);
                    stream.Write(response_bytes, 0, response_bytes.Length);
                }
            }
        }

    }
}
