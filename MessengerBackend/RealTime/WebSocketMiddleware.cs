using System.Net.WebSockets;
using System.Threading.Tasks;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MessengerBackend.RealTime
{
    public class WebSocketMiddleware
    {
        private readonly ILogger<WebSocketMiddleware> _logger;
        private readonly RequestDelegate _next;
        private readonly RealTimeServer _srv;

        public WebSocketMiddleware(RequestDelegate next, RealTimeServer srv, ILogger<WebSocketMiddleware> logger)
        {
            _next = next;
            _srv = srv;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext ctx,
            MessageProcessService messageProcessService)
        {
            if (ctx.Request.Path.StartsWithSegments("/ws"))
            {
                if (ctx.WebSockets.IsWebSocketRequest)
                {
                    try
                    {
                        messageProcessService.Connections = _srv.Connections;
                        await _srv.Connect(await ctx.WebSockets.AcceptWebSocketAsync(),
                            messageProcessService);
                        await _next(ctx);
                    }
                    catch (WebSocketException e) when (e.WebSocketErrorCode ==
                                                       WebSocketError.ConnectionClosedPrematurely)
                    {
                        await _next(ctx);
                    }
                }
                else
                {
                    ctx.Response.StatusCode = 400;
                }
            }
            else
            {
                await _next(ctx);
            }
        }
    }
}