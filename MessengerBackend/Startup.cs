#define USEHMAC

using System;
using AspNetCoreRateLimit;
using MessengerBackend.Database;
using MessengerBackend.Errors;
using MessengerBackend.Policies;
using MessengerBackend.RealTime;
using MessengerBackend.Services;
using MessengerBackend.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Newtonsoft.Json;
using Npgsql.Logging;
using Twilio.Exceptions;
using NewtonsoftJsonException = Newtonsoft.Json.JsonException;

namespace MessengerBackend
{
    public class Startup
    {
        private readonly CryptoService _cryptoService;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _cryptoService = new CryptoService(configuration);
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddMemoryCache();

            // Configure rate limiting
            {
                //load general configuration from appsettings.json
                services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));

                //load ip rules from appsettings.json
                services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

                // inject counter and rules stores
                services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
                services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

                // Add framework services.
                services.AddMvc();

                // https://github.com/aspnet/Hosting/issues/793
                // the IHttpContextAccessor service is not registered by default.
                // the clientId/clientIp resolvers use it.
                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                // configuration (resolvers, counter key builders)
                services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            }
            services.AddSingleton(_cryptoService);

            NpgsqlLogManager.Provider = new SerilogLoggingProvider();

            services.AddDbContext<MessengerDBContext>(builder => builder
                .UseNpgsql(Configuration["Database:ConnectionString"]
                           ?? throw new ArgumentException("No connection string provided"),
                    o => o.SetPostgresVersion(12, 3)));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(cfg =>
            {
                cfg.TokenValidationParameters = _cryptoService.TokenValidationParameters;
            });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .RequireClaim("type", "access")
                    .AddRequirements(new IPCheckRequirement())
                    .Build();
                options.AddPolicy("AuthToken", pol =>
                    pol.AddRequirements(new IPCheckRequirement())
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireClaim("type", "auth"));
            });


            services.AddControllersWithViews();

            services.AddSingleton<VerificationService>();
            services.AddSingleton<RealTimeServer>();

            services.AddScoped<UserService>();
            services.AddScoped<AuthService>();
            services.AddScoped<MessageProcessService>();

            services.AddSwaggerDocument();


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
                app.UseDatabaseErrorPage();
                IdentityModelEventSource.ShowPII = true;
            }
            else
            {
                app.UseHttpsRedirection();
            }

            app.UseIpRateLimiting();

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
                catch (ApiErrorException ex)
                {
                    ctx.Response.StatusCode = ex.HttpStatusCode;
                    ctx.Response.ContentType = "application/json";
                    foreach (var (key, value) in ex.HttpHeaders) ctx.Response.Headers[key] = value;

                    await ctx.Response.WriteAsync(JsonConvert.SerializeObject(new
                    {
                        type = "api",
                        errorCode = ex.Code,
                        summary = ex.Summary,
                        details = ex.Message
                    }));
                }
                catch (ApiException ex)
                {
                    ctx.Response.StatusCode = ex.Status;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsync(JsonConvert.SerializeObject(new
                    {
                        type = "twilio",
                        twilioErrorCode = ex.Code,
                        details = ex.Message,
                        moreInfo = ex.MoreInfo
                    }));
                }
            });

            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2.0)
            });

            app.UseMiddleware<WebSocketMiddleware>();

            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // app.Use(async (ctx, next) =>
            // {
            //     if (!ctx?.GetEndpoint()?.RequestDelegate?.Method
            //         .GetCustomAttributes(typeof(AnyIP), false).Any() ?? false)
            //     {
            //         var ipHash = ctx.User?.FindFirst("ip")?.Value;
            //         if (ipHash != null && !_cryptoService.IPValid(ctx.Connection.RemoteIpAddress, ipHash))
            //             ctx.Response.StatusCode = 400;
            //     }
            //
            //     await next();
            // });

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}