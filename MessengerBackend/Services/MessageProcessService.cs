using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessengerBackend.Models;
using MessengerBackend.RealTime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

// ReSharper disable UnusedMember.Global

namespace MessengerBackend.Services
{
    public class MessageProcessService
    {
        private readonly Random _rng = new Random();
        public readonly ChatService ChatService;
        public readonly UserService UserService;

        public ConcurrentDictionary<ulong, Connection> Connections = null!;
        public Connection Current = null!;
        public ulong CurrentMessageID;

        public MessageProcessService(UserService userService, ChatService chatService)
        {
            UserService = userService;
            ChatService = chatService;
        }

        // All public methods here are callable by the websocket Method property
        // They must return Task<OutboundMessage> or OutboundMessage

        public OutboundMessage Echo(string data) => new OutboundMessage
        {
            Data = new Dictionary<string, object?> { { "Echo", data } },
            Type = OutboundMessageType.Response,
            IsSuccess = true
        };

        [Authorize]
        public async Task<OutboundMessage> CreateChatGroup(string name)
        {
            var room = new Room
            {
                Name = name,
                CreatedAt = DateTime.UtcNow,
                Type = RoomType.Group
            };
            var newRoom = await ChatService.CreateRoomWithUser(room, Current.User!);
            return Success(new Dictionary<string, object?>
            {
                { "roomID", newRoom.RoomPID }
            });
        }

        [Authorize]
        public async Task<OutboundMessage> CreatePrivateChat(string targetPID)
        {
            var room = new Room
            {
                CreatedAt = DateTime.UtcNow,
                Type = RoomType.Private
            };
            var newRoom = await ChatService.CreateRoomWithUser(room, Current.User!);
            var targetUser = await
                UserService.Users.SingleOrDefaultAsync(u => u.UserPID == targetPID);
            if (targetUser == null)
            {
                return Fail("User not found");
            }

            await ChatService.AddUserToRoom(newRoom, targetUser);
            return Success(new Dictionary<string, object?>
            {
                { "roomID", newRoom.RoomPID }
            });
        }

        [Authorize]
        public async Task<OutboundMessage> CreateChannel(string name, string link)
        {
            var room = new Room
            {
                Name = name,
                CreatedAt = DateTime.UtcNow,
                Type = RoomType.Channel,
                Link = link
            };
            if (ChatService.Rooms.Any(r => r.Link == link))
            {
                return Fail("Link must be unique");
            }

            var newRoom = await ChatService.CreateRoomWithUser(room, Current.User!);
            return Success(new Dictionary<string, object?>
            {
                { "roomID", newRoom.RoomPID },
                { "link", link }
            });
        }

        [Authorize]
        public async Task<OutboundMessage> SendMessage(string text, string roomPID)
        {
            var room = await ChatService.Rooms.Include(r => r.Participants)
                .SingleOrDefaultAsync(r => r.RoomPID == roomPID);
            if (room == null)
            {
                return Fail("Room not found");
            }

            var msg = await ChatService.CreateMessage(new Message
            {
                Sender = Current.User,
                Text = text,
                TargetRoom = room
            });
            if (room == null || room.Users.All(u => u.UserID != Current.User!.UserID))
            {
                return Fail("Access denied");
            }

            foreach (var sub in
                ChatService.Subscriptions.Include(s => s.User)
                    .Where(s => s.Room.RoomID == room.RoomID))
            {
                var ev = await ChatService.CreateEvent(new Event
                {
                    Message = msg,
                    Subscription = sub
                });
                foreach (var connection in Connections.Values.Where(c => c.User?.UserID == sub.User.UserID))
                {
                    var id = (uint) (_rng.NextDouble() * uint.MaxValue);
                    connection.AnswerPendingMessages.TryAdd(CurrentMessageID, async message =>
                        await ChatService.DeliverEvent(ev)
                    );
                    await connection.OutboundMessages.Writer.WriteAsync(new OutboundMessage
                    {
                        Type = OutboundMessageType.Event,
                        Data = new Dictionary<string, object?>
                        {
                            { "eventType", "message" },
                            { "room", roomPID },
                            { "subscription", sub.SubscriptionPID },
                            { "replyTo", null },
                            { "text", text }
                        },
                        IsSuccess = true,
                        ID = id
                    });
                }
            }

            return Success(new Dictionary<string, object?>
            {
                { "messagePID", msg.MessagePID }
            });
        }

        private static OutboundMessage Fail(string error) => new OutboundMessage
        {
            Data = new Dictionary<string, object?> { { "error", error } },
            IsSuccess = false,
            Type = OutboundMessageType.Response
        };

        private OutboundMessage NotImplemented(uint id) => Fail("Not implemented");

        private static OutboundMessage Success() => new OutboundMessage
        {
            IsSuccess = true,
            Type = OutboundMessageType.Response
        };

        private static OutboundMessage Success(Dictionary<string, object?>? data) => new OutboundMessage
        {
            IsSuccess = true,
            Data = data,
            Type = OutboundMessageType.Response
        };
    }
}