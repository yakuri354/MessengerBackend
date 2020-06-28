using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dotenv.net;
using JWT.Algorithms;
using JWT.Builder;
using MessengerBackend.RealTime;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MessengerBackend
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
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
            
            if (Environment.GetEnvironmentVariable("NO_TWILIO") == null)
            {
                Log.Information("Initializing Twilio");
            }
            
            rtask?.Wait();
            Log.Information("Exiting");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

    }
}