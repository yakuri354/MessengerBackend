using DotNetty.Transport.Channels;

namespace MessengerBackend.Sockets.Models
{
    // A representation of the connected Clients on the server.  
    // Note:  This is not the 'Client' class that would be used to communicate with the server.
    public class SocketClient
    {
        private readonly string _clientId;
        private readonly IChannelHandlerContext _context;

        public SocketClient(string clientId, IChannelHandlerContext context)
        {
            this._clientId = clientId;
            this._context = context;
        }

        public string ClientId => _clientId;
        public IChannelHandlerContext Context => _context;
    }

}