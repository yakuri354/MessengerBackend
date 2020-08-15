using System;
using System.Threading.Tasks;
using MessengerBackend.Database;
using MessengerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace MessengerBackend.Services
{
    public class ChatService
    {
        private readonly MessengerDBContext _dbContext;
        public ChatService(MessengerDBContext dbContext) => _dbContext = dbContext;
        public DbSet<Room> Rooms => _dbContext.Rooms;
        public DbSet<Subscription> Subscriptions => _dbContext.Subscriptions;

        public async Task<Room> CreateRoomWithUser(Room room, User user)
        {
            var nRoom = await _dbContext.Rooms.AddAsync(room);
            await _dbContext.RoomParticipants.AddAsync(new RoomParticipant
            {
                Role = ParticipantRole.Creator,
                Room = room,
                User = user
            });
            await _dbContext.SaveChangesAsync();
            return nRoom.Entity;
        }

        public async Task AddUserToRoom(Room room, User user)
        {
            var participant = new RoomParticipant
            {
                Role = ParticipantRole.Participant,
                User = user,
                Room = room
            };
            await _dbContext.RoomParticipants.AddAsync(participant);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Message> CreateMessage(Message message)
        {
            var msg = await _dbContext.Messages.AddAsync(message);
            await _dbContext.SaveChangesAsync();
            return msg.Entity;
        }


        public async Task<Subscription?> Unsubscribe(string subPID, User user)
        {
            var sub = await _dbContext.Subscriptions
                .SingleOrDefaultAsync(s => s.SubscriptionPID == subPID
                                           && s.User.UserID == user.UserID);
            if (sub == null)
            {
                return sub;
            }

            _dbContext.Subscriptions.Remove(sub);
            await _dbContext.SaveChangesAsync();
            return sub;
        }

        public async Task<Subscription> Subscribe(Subscription subscription)
        {
            var sub = await _dbContext.Subscriptions.AddAsync(subscription);
            await _dbContext.SaveChangesAsync();
            return sub.Entity;
        }

        public async Task<Event> CreateEvent(Event @event)
        {
            var ev = await _dbContext.Events.AddAsync(@event);
            await _dbContext.SaveChangesAsync();
            return ev.Entity;
        }

        public async Task<Event> DeliverEvent(Event @event)
        {
            var ev = await _dbContext.Events
                .SingleOrDefaultAsync(e => e.EventID == @event.EventID);
            if (ev.DeliveredAt != null)
            {
                return ev;
            }

            ev.DeliveredAt = DateTime.UtcNow;
            _dbContext.Attach(ev);
            await _dbContext.SaveChangesAsync();
            return ev;
        }
    }
}