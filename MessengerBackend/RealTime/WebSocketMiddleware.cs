using System.Net.WebSockets;
using System.Threading.Tasks;
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

        public async Task InvokeAsync(HttpContext ctx)
        {
            if (ctx.Request.Path.StartsWithSegments("/ws"))
            {
                if (ctx.WebSockets.IsWebSocketRequest)
                {
                    var tcs = new TaskCompletionSource<object>();
                    _srv.ProcessNewConnectionAsync(await ctx.WebSockets.AcceptWebSocketAsync(), tcs);
                    await tcs.Task;
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