using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using MessengerBackend.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MessengerBackend.RealTime
{
    public class OpenConnection
    {
        public EntityEntry<User> User;
        public WebSocket Socket;
        public Guid ConnectionID = Guid.NewGuid();
        public TaskCompletionSource<object> TaskCompletionSource;
    }
}