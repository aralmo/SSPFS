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

        int retry = 20;

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
                    string url = request_remote_url();

                    Program.Form.Invoke(new MethodInvoker(() =>
                    {
                        Program.Form.tburl.Text = url;
                    }));

                    Log.LogMessage("Conectado");
                    return client.Connected;
                }
                else
                {
                    if ((DateTime.Now - last_keepalive_sent).TotalSeconds > 2)
                    {
                        send_keepalive();
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

        private string request_remote_url()
        {
            long command = 3;
            byte[] packet_bytes = new byte[16].Concat(BitConverter.GetBytes(command)).ToArray();
            client.GetStream().Write(packet_bytes, 0, packet_bytes.Length);

            //esperamos a que haya datos disponibles y devolvemos la URL.
            while (client.Available <= 0)
            {
                //esperamos a recibir la respuesta del servidor
                Thread.Sleep(100);
            }

            var stream = client.GetStream();
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            int length = BitConverter.ToInt32(buffer, 0);
            buffer = new byte[length];
            stream.Read(buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }

        private void send_keepalive()
        {
            lock (host_data_connection_lock)
            {
                long command = 2;
                byte[] packet_bytes = new byte[16].Concat(BitConverter.GetBytes(command)).ToArray();
                client.GetStream().Write(packet_bytes, 0, packet_bytes.Length);
            }
        }

        private void ProcessIncomingPacket(Guid request_identifier, int code, long length, NetworkStream stream)
        {



            //listar ficheros
            switch (code)
            {
                case 1:
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
                        stream.Write(request_identifier.ToByteArray().Concat(BitConverter.GetBytes(response_length)).ToArray(), 0, 24);
                        stream.Write(response_bytes, 0, response_bytes.Length);
                    }
                    break;
                case 4:

                    byte[] filename_bytes = new byte[length];
                    stream.Read(filename_bytes, 0, (int)length);

                    string filename = Path.Combine(folder, Encoding.UTF8.GetString(filename_bytes));
                    int readen = 0, this_read;
                    using (var fs = File.OpenRead(filename))
                    {
                        byte[] file_bytes = new byte[4096];
                        lock (host_data_connection_lock)
                        {
                            stream.Write(request_identifier.ToByteArray().Concat(BitConverter.GetBytes(fs.Length)).ToArray(), 0, 24);
                            while (readen < fs.Length)
                            {
                                this_read = fs.Read(file_bytes, 0, 4096);
                                readen += this_read;

                                stream.Write(file_bytes, 0, this_read);
                            }
                        }
                    }
                    break;
            }
        }

    }
}
