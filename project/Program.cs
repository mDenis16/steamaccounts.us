using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace WebApplication1
{

    public class Program
    {
        private static string pathToContentRoot;
        public static bool IsLinux()
        {
            int p = (int)Environment.OSVersion.Platform;
            return (p == 4) || (p == 6) || (p == 128);

        }
        public static void Main(string[] args)
        {

            if (IsLinux())
            {
                CreateHostBuilder(args).Build().Run();
                Console.WriteLine("STEAMACCOUNTS.US STARTED FOR LINUX");
            }
            else
            {
                Console.WriteLine("STEAMACCOUNTS.US STARTED FOR WINDOWS");
               

                pathToContentRoot = Directory.GetCurrentDirectory();
               
                    var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                    pathToContentRoot = Path.GetDirectoryName(pathToExe);
                
                Console.WriteLine("Running demo with HTTP.sys.");

                BuildWebHost(args).Run();

            }
        }
        public static IWebHost BuildWebHost(string[] args) =>
                 WebHost.CreateDefaultBuilder(args)

                .UseHttpSys(options =>
               {
                   options.Authentication.AllowAnonymous = true;
                   options.Authentication.Schemes = Microsoft.AspNetCore.Server.HttpSys.AuthenticationSchemes.NTLM;
                   options.MaxConnections = 1000;
                   options.MaxRequestBodySize = 300000000;
                   options.UrlPrefixes.Add("http://localhost:80");
               })

                .UseContentRoot(pathToContentRoot)
                     .UseStartup<Startup>()
                     .Build();


        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}



