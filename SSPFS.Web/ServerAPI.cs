using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Lista los ficheros en el cliente
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public IEnumerable<RemoteFile> ListFiles(Guid identifier)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sube un fichero al cliente
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="filename"></param>
        /// <param name="file"></param>
        public void UploadFile(Guid identifier, string filename, Stream file)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Descarga un fichero desde el cliente
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="filename"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public Stream DownloadFile(Guid identifier, string filename, Stream file)
        {
            throw new NotImplementedException();
        }
    }
}
