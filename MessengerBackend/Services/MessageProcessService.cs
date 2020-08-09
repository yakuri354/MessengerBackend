using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MessengerBackend.Models;
using MessengerBackend.RealTime;
using Microsoft.AspNetCore.Authorization;

// ReSharper disable UnusedMember.Global

namespace MessengerBackend.Services
{
    public class MessageProcessService
    {
        public readonly UserService UserService;
        public readonly ChatService ChatService;

        public User Caller = null!;

        public MessageProcessService(UserService userService, ChatService chatService)
        {
            UserService = userService;
            ChatService = chatService;
        }

        // All public methods here are callable by the websocket Method property
        // They must return Task<OutboundMessage> or OutboundMessage

        public OutboundMessage Echo(string data) => new OutboundMessage
        {
            Data = new Dictionary<string, object> { { "Echo", data } },
            Type = OutboundMessageType.Response,
            IsSuccess = true
        };

        [Authorize]
        public async Task<OutboundMessage> CreateChatRoom(string name, string roomType, string? link)
        {
            var parsed = Enum.TryParse(typeof(RoomType), roomType, out var rawType);
            if (!parsed || rawType == null) throw new ProcessException($"Room type {roomType} is invalid");
            var type = (RoomType) rawType;
            if (type == RoomType.Channel && link == null)
                throw new ProcessException("Link must be specified when creating a channel");
            var room = new Room
            {
                Name = name,
                CreatedAt = DateTime.UtcNow,
                Type = type
            };
            if (type == RoomType.Channel)
                room.Link = link;
            var newRoom = await ChatService.CreateRoomWithUser(room, Caller);
            var data = new Dictionary<string, object>
            {
                { "roomID", newRoom.RoomPID }
            };
            if (type == RoomType.Channel)
                data["link"] = link!;
            return Success(data);
        }

        [Authorize]
        public async Task<Message> SendMessage(string text, string recepientPID)
        {
            var msg = await ChatService.CreateMessage(new Message
            {
                Sender = Caller,
                Text = text
            });
            // TODO Notify
            return msg;
        }

        private static OutboundMessage Success(Dictionary<string, object>? data) => new OutboundMessage
        {
            Type = OutboundMessageType.Response,
            IsSuccess = true,
            Data = data
        };
    }

    public class ProcessException : Exception
    {
        public ProcessException(string message) : base(message)
        {
        }
    }
}