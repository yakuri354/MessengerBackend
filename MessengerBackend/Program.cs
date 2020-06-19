using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dotenv.net;
using MessengerBackend.RealTime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Serilog;

namespace MessengerBackend
{
    public static class Program
    {
        public static IMongoDatabase DB { get; private set; }
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();
            Log.Information("Starting");
            DotEnv.Config();
            Log.Information("Initializing database");
            DB = InitializeDatabase();
            Task rtask = null;
            if (Environment.GetEnvironmentVariable("ASPNET_ONLY") == null)
            {
                Log.Information("Starting RealTime server");
                rtask = new RealTimeServer(new CancellationTokenSource()).Start();
            }

            if (Environment.GetEnvironmentVariable("REALTIME_ONLY") == null)
            {
                Log.Information("Starting ASP.NET framework");
                CreateHostBuilder(args).Build().Run();
            }

            rtask?.Wait();
            Log.Information("Exiting");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

        private static IMongoDatabase InitializeDatabase()
        {
            return new MongoClient(Environment.GetEnvironmentVariable("MONGO_CONNSTRING"))
                .GetDatabase(Environment.GetEnvironmentVariable("MONGO_DBNAME"));
        }
    }
}