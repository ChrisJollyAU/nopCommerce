﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Nop.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = WebHost.CreateDefaultBuilder(args)
                .UseApplicationInsights()
                .UseKestrel(options => options.AddServerHeader = false)
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
