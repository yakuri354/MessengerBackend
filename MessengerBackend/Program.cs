using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace MessengerBackend
{
    public static class Program
    {
        public static LoggerConfiguration Configuration { get; private set; } = null!;

        public static async Task<int> Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            Configuration = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Npgsql", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(host.Services.GetService<IConfiguration>());

            Log.Logger = Configuration.CreateLogger();

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

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>().UseKestrel());
        }
    }
}