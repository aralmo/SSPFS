using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SSPFS.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //iniciamos el host que va a escuchar por las solicitudes de compartir de los clientes de escrotorio
            ServerHost host = new ServerHost(new System.Net.IPEndPoint(IPAddress.Any, 1666));
            host.StartListening();

            //webapp
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
