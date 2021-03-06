﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SSPFS.Web
{
    public class Program
    {

        public static IServiceProvider Services { get; private set;}

        public static void Main(string[] args)
        {
            //iniciamos el host que va a escuchar por las solicitudes de compartir de los clientes de escrotorio
            ServerAPI.Current = new ServerAPI();            
            ServerHost host = new ServerHost(new System.Net.IPEndPoint(IPAddress.Any, 1666));
            ServerHost.Current = host;
            host.Start();
                
            //webapp
            var webHost = BuildWebHost(args);
            Services = webHost.Services;
            webHost.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)          
                .UseStartup<Startup>()
                .UseKestrel()
                .UseIISIntegration()
                .Build();
    }
}
