using System;
using DotNetty.Transport.Channels;

namespace MessengerBackend.Sockets.Models
{
    public class RawSocketMessage
    {
        public byte[] Message;
    }

    public class UnhandledRawSocketMessage
    {
        public RawSocketMessage SocketMessage;
        public IChannelHandlerContext Context;

        public UnhandledRawSocketMessage(RawSocketMessage r, IChannelHandlerContext c)
        {
            SocketMessage = r;
            Context = c;
        }
    }
    public interface BasicSocketMessage
    {
        public SocketMessageCommand Command { get; }
        public bool IsInbound { get; } // If user sends message to server or vice versa
        public string Payload { get; }
        

        // public BasicSocketMessage( SocketMessageCommand c, bool isInbound, string payload)
        // {
        //     Command = c;
        //     IsInbound = isInbound;
        //     Payload = payload;
        // }
    }

    namespace MessengerBackend.Sockets.Models.SocketMessageTypes
    {
        class AuthorizationSocketMessage : BasicSocketMessage
        {
            public SocketMessageCommand Command { get; } = SocketMessageCommand.Authorization;
            public string Payload { get; } // This is JWT
            public bool IsInbound { get; } = true;

            public AuthorizationSocketMessage(string jwt)
            {
                Payload = jwt;
            }
        }
    }
    
    // public class UnhandledSocketMessage
    // {
    //     private readonly SocketMessage _socketMessage;
    //
    //     public UnhandledSocketMessage(SocketMessage socketMessage, IChannelHandlerContext context)
    //     {
    //         _socketMessage = socketMessage;
    //         Context = context;
    //     }
    //
    //     public SocketMessage SocketMessage => _socketMessage;
    //     public IChannelHandlerContext Context { get; }
    //
    //     public SocketMessageCommand Command => _socketMessage.Command;
    //     public string ClientId => _socketMessage.ClientId;
    //     public string Data => _socketMessage.Data;
    // }
}