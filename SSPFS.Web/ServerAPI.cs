using SSPFS.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSPFS
{
    public class ServerAPI
    {
        public static ServerAPI Current { get; internal set; }

        public delegate void RemoteFolderHasChangedEventHandler(Guid identifier);
        /// <summary>
        /// Indica que se producieron cambios en la carpeta remota.
        /// </summary>
        public event RemoteFolderHasChangedEventHandler RemoteFolderHasChanged;

        public delegate void RemoteFolderDisconnectedEventHandler(Guid identifier);
        /// <summary>
        /// Indica que se desconecto la carpeta remota del servidor.
        /// </summary>
        public event RemoteFolderDisconnectedEventHandler RemoteFolderDisconnected;

        string BasePath = "/home/yoni/Documentos/Repos/Docs";

        /// <summary>
        /// Método que reporta a la api de servidor cuando ha cambiado el contenido de uno de los hosts
        /// </summary>
        /// <param name="client_identifier"></param>
        public void ReportRemoteHostChanged(Guid client_identifier)
        {
            RemoteFolderHasChanged?.Invoke(client_identifier);
        }

        public void ReportRemoteHostDisconnect(Guid client_identifier)
        {
            RemoteFolderDisconnected?.Invoke(client_identifier);
        }

        /// <summary>
        /// Lista los ficheros en el cliente
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RemoteFile>> ListFiles(Guid identifier)
        {
            var client = ServerHost.Current.Hosts[identifier];
            if (client == null)
                throw new Exception("El cliente especificado no existe.");

            bool is_finished = false;
            IEnumerable<RemoteFile> result = null;

            var task = Task<IEnumerable<RemoteFile>>.Run(() =>
            {
                DateTime start_time = DateTime.Now;

                while (!is_finished)
                {
                    Thread.Sleep(10);

                    if ((DateTime.Now - start_time).TotalMinutes >= 1)
                        throw new TimeoutException("ListFiles");
                }
                client.CurrentStatus = HostClientStatusEnum.Listening;
                return result;
            });

            client.EnqueueRequest(PacketBuilder.ListFilesForClientPacket(), (stream, length) =>
             {
                 if (length > int.MaxValue)
                     throw new Exception("Too much to handle");

                 byte[] content = new byte[length];
                 stream.Read(content, 0, (int)length);
                 result = Encoding.UTF8.GetString(content, 0, (int)length).Split(new char[] { '|' })
                            .Select(x => new RemoteFile(x));

                 is_finished = true;
             });

            return await task;
        }

        /// <summary>
        /// Sube un fichero al cliente
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="filename"></param>
        /// <param name="file"></param>
        public void UploadFile(Guid identifier, string filename, Stream file)
        {
            using (var result = File.Create(Path.Combine(BasePath, identifier.ToString(), filename)))
                file.CopyTo(result);
        }

        /// <summary>
        /// Descarga un fichero desde el cliente
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="filename"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public struct DownloadFileResult
        {
            public Stream stream;
            public int length;
            public Action RequestContentDownloadedTrigger;
        }

        public async Task<DownloadFileResult> DownloadFile(Guid identifier, string filename)
        {

            var client = ServerHost.Current.Hosts[identifier];
            if (client == null)
                throw new Exception("El cliente especificado no existe.");

            bool is_finished = false;

            Stream result = null;
            int result_length = 0;

            var task = Task<Stream>.Run(() =>
            {
                DateTime start_time = DateTime.Now;

                while (!is_finished)
                {
                    Thread.Sleep(10);

                    if ((DateTime.Now - start_time).TotalMinutes >= 1)
                        throw new TimeoutException("Download File");
                }

                return new DownloadFileResult()
                {
                    stream = result,
                    length = result_length,
                    RequestContentDownloadedTrigger = () =>
                    {
                        client.CurrentStatus = HostClientStatusEnum.Listening;
                    }
            };
        });

            client.EnqueueRequest(PacketBuilder.RequestFileForDownload(filename) , (stream, length) =>
            {
                if (length > int.MaxValue)
                    throw new Exception("Too much to handle");

        result = stream;
                result_length = (int) length;
        is_finished = true;
            });

            return await task;
}

/// <summary>
/// Hace una solicitud al cliente indicado y ejecuta el callback pasado como parámetro al ser respondida.
/// </summary>
/// <param name="request"></param>
/// <param name="callback"></param>
public static void RequestToClient(Guid client_id, byte[] request, ServerHost.ProcessClientResponseDelegateHandler callback)
{
    ServerHost.Current.Hosts.TryGetValue(client_id, out HostClient client);
    client.EnqueueRequest(request, callback);
}
    }
}
