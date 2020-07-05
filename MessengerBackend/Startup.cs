#define USEHMAC

using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using MessengerBackend.Database;
using MessengerBackend.Services;
using MessengerBackend.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Npgsql.Logging;
using NewtonsoftJsonException = Newtonsoft.Json.JsonException;

namespace MessengerBackend
{
    public class Startup
    {
        private CryptoService _cryptoService;

        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            _cryptoService = new CryptoService(Configuration);
            services.AddSingleton(_cryptoService);
            NpgsqlLogManager.Provider = new SerilogLoggingProvider();
            services.AddDbContext<MessengerDBContext>(builder => builder
                .UseNpgsql(Configuration["Database:ConnectionString"]
                           ?? throw new ArgumentException("No connection string provided"),
                    o => o.SetPostgresVersion(12, 3)));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(cfg =>
            {
                cfg.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = CryptoService.JwtOptions.Issuer,
                    ValidateAudience = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    ValidAudience = CryptoService.JwtOptions.Audience,
#if USERSA
                    IssuerSigningKey = new RsaSecurityKey(cryptoService.PublicKey)
#elif USEHMAC
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cryptoService.HMACKey))
#endif
                };
            });


            services.AddControllersWithViews();

            services.AddSingleton<VerificationService>();

            services.AddScoped<UserService>();
            services.AddScoped<AuthService>();

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

            services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            NpgsqlLogManager.Provider = new ConsoleLoggingProvider();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                IdentityModelEventSource.ShowPII = true;
            }
            else
            {
                app.UseHttpsRedirection();
            }

            app.Use(async (ctx, next) =>
            {
                await next();

                if (ctx.Response.StatusCode == 404 && !ctx.Response.HasStarted)
                {
                    //Re-execute the request so the user gets the error page
                    var originalPath = ctx.Request.Path.Value;
                    ctx.Items["originalPath"] = originalPath;
                    ctx.Request.Path = "/error/404";
                    await next();
                }
            });

            app.Use(async (ctx, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception e) when (e is JsonException || e is NewtonsoftJsonException)
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    var json = JsonSerializer.Serialize(new { error = "invalid json", message = e.Message });
                    await ctx.Response.WriteAsync(json);
                }
            });
            // app.UseOpenApi();
            // app.UseSwaggerUi3();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(async (ctx, next) =>
            {
                if (!ctx?.GetEndpoint()?.RequestDelegate?.Method
                    .GetCustomAttributes(typeof(AnyIP), false).Any() ?? false)
                {
                    var ipHash = ctx.User?.FindFirst("ip")?.Value;
                    if (ipHash != null && !_cryptoService.IPValid(ctx.Connection.RemoteIpAddress, ipHash))
                        ctx.Response.StatusCode = 400;
                }

                await next();
            });

            // TODO Rate limiting

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}