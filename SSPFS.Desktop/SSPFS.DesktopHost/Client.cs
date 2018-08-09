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
        const string Host = "euve255872.serverprofi24.net";
        //const string Host = "localhost";
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
                            Program.Form.lbEstado.Text = "Conectando...";
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
                    Log.LogMessage($"Resolviendo host {Host}");

                    var ip = Dns.Resolve(Host);
                    client.Connect(new IPEndPoint(ip.AddressList.FirstOrDefault()?.MapToIPv4(), Port));
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
                    Log.LogMessage("-> Listado de ficheros.");

                    //listar fichero.
                    if (length > 0)
                        throw new Exception("Bad code 0");

                    //montamos el paquete respuesta

                    var files = string.Join("|", Directory.GetFiles(folder).Select(x => Path.GetFileName(x)));
                    
                    //Si está marcado el check permitimos subida de ficheros.
                    if (Program.Form.cbPermitirSubida.Checked)
                    {
                        files = "#upload|" + files;
                    }
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

                    Log.LogMessage($"-> Enviando fichero {filename}");


                    int readen = 0, this_read;
                    using (var fs = File.OpenRead(filename))
                    {
                        byte[] file_bytes = new byte[4096];
                        lock (host_data_connection_lock)
                        {
                            stream.Write(request_identifier.ToByteArray().Concat(BitConverter.GetBytes(fs.Length)).ToArray(), 0, 24);
                            while (readen < fs.Length)
                            {
                                this_read = fs.Read(file_bytes, 0,Math.Min((int)fs.Length - readen,file_bytes.Length));
                                readen += this_read;

                                stream.Write(file_bytes, 0, this_read);
                            }
                        }
                    }
                    break;
                case 5:
                    if (!Program.Form.cbPermitirSubida.Checked)
                    {
                        Log.LogError("Se intentó subir un fichero sin tener el permiso habilitado, cerrando Socket");
                        stream.Flush();
                        return;
                    }

                    lock (host_data_connection_lock)
                    {
                        byte[] blong_nombre_fichero = new byte[4];
                        byte[] blongitud_fichero = new byte[8];
                        byte[] bnombre_fichero;
                        long longitud_fichero;
                        string nombre_fichero;

                        stream.Read(blong_nombre_fichero, 0, 4);
                        stream.Read(blongitud_fichero, 0, 8);

                        bnombre_fichero = new Byte[BitConverter.ToInt32(blong_nombre_fichero, 0)];
                        stream.Read(bnombre_fichero, 0, bnombre_fichero.Length);
                        nombre_fichero = Encoding.UTF8.GetString(bnombre_fichero);
                        longitud_fichero = BitConverter.ToInt64(blongitud_fichero, 0);

                        //calcula un nombre para el fichero
                        string full_filename = get_new_filename(folder, nombre_fichero);

                        Log.LogMessage($"-> Descargando fichero {nombre_fichero}");

                        int readen_bytes = 0;
                        int this_read_bytes = 0;
                        byte[] buffer = new byte[4096];
                        int tamaño_fichero = (int) longitud_fichero;
                        using (var fs = File.Create(full_filename))
                        {
                            while (readen_bytes < longitud_fichero)
                            {
                                this_read_bytes = stream.Read(buffer, 0,Math.Min(tamaño_fichero - readen_bytes,buffer.Length));
                                readen_bytes += this_read_bytes;

                                fs.Write(buffer, 0, this_read_bytes);
                            }
                        }
                    }
                    break;
            }
        }

        private string get_new_filename(string folder, string nombre_fichero)
        {
            if (File.Exists(Path.Combine(folder, nombre_fichero)))
            {
                string extension = Path.GetExtension(nombre_fichero);
                string filename = Path.GetFileNameWithoutExtension(nombre_fichero);
                int n = 1;

                while (File.Exists(Path.Combine(folder, $"{filename}_{n}{extension}")))
                    n++;

                return Path.Combine(folder, $"{filename}_{n}{extension}");
            }
            else
            {
                return Path.Combine(folder, nombre_fichero);
            }
        }

        public void ReportDirectoryChangesToServer()
        {
            lock (host_data_connection_lock)
            {
                long command = 1;
                byte[] packet_bytes = new byte[16].Concat(BitConverter.GetBytes(command)).ToArray();
                client.GetStream().Write(packet_bytes, 0, packet_bytes.Length);
            }
        }
    }
}
