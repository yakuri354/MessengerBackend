using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace MessengerBackend
{
    public static class Program
    {
        public static LogEventLevel LogEventLevel = LogEventLevel.Debug;

        public static LoggerConfiguration LoggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Npgsql", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console();

        public static async Task<int> Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            Log.Logger = LoggerConfiguration.CreateLogger();

            try
            {
                Log.Information("Starting web host");
                await host.RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>().UseKestrel());
        }

        public class TestClass
        {
            public static int Add(int a, int b) => a + b;
        }
    }
}