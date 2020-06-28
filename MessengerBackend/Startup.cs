using System;
using JWT;
using MessengerBackend.Models;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql.Logging;
using Serilog;

namespace MessengerBackend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSingleton<UserService>();
            services.AddSingleton<VerificationService>();

            services.AddSwaggerDocument();

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.ClientErrorMapping[429] = new ClientErrorData
                {
                    Title = "Too Many Requests"
                };
            });

            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

            Log.Information("Initializing Postgres");
            services.AddDbContext<MessengerDBContext>(builder => builder
                .UseNpgsql(Environment.GetEnvironmentVariable("POSTGRES_CONNSTRING")
                           ?? throw new ArgumentException("No POSTGRES_CONNSTRING provided"),
                    o => o.SetPostgresVersion(12, 3)));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            NpgsqlLogManager.Provider = new ConsoleLoggingProvider();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseRouting();

            app.UseAuthorization();
            app.UseAuthentication();
            
            // TODO Rate limiting

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}