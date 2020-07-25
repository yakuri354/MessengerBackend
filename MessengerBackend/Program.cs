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

            // if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            //     logBuilder.MinimumLevel.Debug();
            // else logBuilder.MinimumLevel.Information();

            Log.Logger = logBuilder
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("Starting");

            CreateHostBuilder(args).Build().Run();

            Log.Information("Exiting");
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>().UseKestrel());
        }
    }
}