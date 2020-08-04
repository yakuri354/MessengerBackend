using System.Net.WebSockets;
using System.Threading.Tasks;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Http;

namespace MessengerBackend.RealTime
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RealTimeServer _srv;

        public WebSocketMiddleware(RequestDelegate next, RealTimeServer srv)
        {
            _next = next;
            _srv = srv;
        }

        public async Task InvokeAsync(HttpContext ctx, UserService userService, ChatService chatService)
        {
            if (ctx.Request.Path.StartsWithSegments("/ws"))
            {
                if (ctx.WebSockets.IsWebSocketRequest)
                {
                    try
                    {
                        await _srv.Connect(await ctx.WebSockets.AcceptWebSocketAsync(), userService, chatService);
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