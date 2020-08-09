using System.Threading.Tasks;
using MessengerBackend.Database;
using MessengerBackend.Models;

namespace MessengerBackend.Services
{
    public class ChatService
    {
        private MessengerDBContext _dbContext;
        public ChatService(MessengerDBContext dbContext) => _dbContext = dbContext;

        public async Task<Room> CreateRoomWithUser(Room room, User user)
        {
            var nRoom = await _dbContext.Rooms.AddAsync(room);
            await _dbContext.RoomParticipants.AddAsync(new RoomParticipant
            {
                Role = ParticipantRole.Creator,
                Room = room,
                User = user
            });
            return nRoom.Entity;
        }

        public async Task<Message> CreateMessage(Message message) =>
            (await _dbContext.Messages.AddAsync(message)).Entity;
        public async Task<Subscription> CreateSubscription(Subscription sub) =>
            (await _dbContext.Subscriptions.AddAsync(sub)).Entity;

        public async Task Subscribe(string channel, User user)
        {
            
        }
    }
}