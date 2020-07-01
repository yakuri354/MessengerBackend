using System;
using System.Threading;
using System.Threading.Tasks;
using MessengerBackend.RealTime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MessengerBackend
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var logBuilder = new LoggerConfiguration();

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                logBuilder.MinimumLevel.Debug();
            else logBuilder.MinimumLevel.Information();

            Log.Logger = logBuilder
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("Starting");

            Task rtask = null;
            if (Environment.GetEnvironmentVariable("NO_REALTIME") == null)
            {
                Log.Information("Starting RealTime server");
                rtask = new RealTimeServer(new CancellationTokenSource()).Start();
            }

            if (Environment.GetEnvironmentVariable("NO_ASPNET") == null)
            {
                Log.Information("Starting ASP.NET framework");
                CreateHostBuilder(args).Build().Run();
            }

            rtask?.Wait();
            Log.Information("Exiting");
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>().UseKestrel(); });
        }
    }
}