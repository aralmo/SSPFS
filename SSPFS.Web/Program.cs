using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
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
            ServerAPI.Current = new ServerAPI();
            ServerHost host = new ServerHost(new System.Net.IPEndPoint(IPAddress.Any, 1666));
            ServerHost.Current = host;
            host.Start();
            bool notdone = true;
            while (true)
            {
                System.Threading.Thread.Sleep(100);
                if (host.Hosts.Any())
                {
                    if (notdone)
                    {
                        var list_files_task = ServerAPI.Current.ListFiles(host.Hosts.First().Key);
                        while (!list_files_task.GetAwaiter().IsCompleted)
                        {
                            Thread.Sleep(100);
                        }

                        foreach (var file in list_files_task.Result)
                        {

                        }
                        notdone = false;
                    }
                }
            }
                
            //webapp
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
