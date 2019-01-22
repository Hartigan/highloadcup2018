using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AspNetCoreWebApi.Processing;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using AspNetCoreWebApi.Storage.Contexts;
using AspNetCoreWebApi.Storage;

namespace AspNetCoreWebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();

            var messageProcessor = host.Services.GetRequiredService<MessageProcessor>();

            var loader = host.Services.GetRequiredService<DataLoader>();
            loader.Config("../../highloadcup2018_data/data/options.txt");
            loader.Run("../../highloadcup2018_data/data/data.zip");

            //loader.Config("/tmp/data/options.txt");
            //loader.Run("/tmp/data/data.zip");

            var context = host.Services.GetRequiredService<MainContext>();
            var storage = host.Services.GetRequiredService<MainStorage>();

            var res = GC.TryStartNoGCRegion(1024 * 1024);

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options => {
                    options.Listen(IPAddress.Any, 8080);
                })
                .UseStartup<Startup>();
    }
}
