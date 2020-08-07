using System.Collections.Generic;
using MessengerBackend.RealTime;
// ReSharper disable UnusedMember.Global

namespace MessengerBackend.Services
{
    public class MessageProcessService
    {
        public UserService UserService;
        private ChatService _chatService;
        
        public MessageProcessService(UserService userService, ChatService chatService)
        {
            UserService = userService;
            _chatService = chatService;
        }

        // All public methods here are callable by the websocket Method property
        // They must return Task<OutboundMessage> or OutboundMessage

        public OutboundMessage Echo(string data) => new OutboundMessage
        {
            Data = new Dictionary<string, object> { { "Echo", data } },
            Type = OutboundMessageType.Response,
            IsSuccess = true
        };
    }
}