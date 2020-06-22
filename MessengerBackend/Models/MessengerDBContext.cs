using System;
using Microsoft.EntityFrameworkCore;

namespace MessengerBackend.Models
{
    public class MessengerDBContext : DbContext
    {
        public DbSet<User> Users;
        public DbSet<Room> Rooms;
        public DbSet<Message> Messages;
        public DbSet<Session> Sessions;
        
        public MessengerDBContext(DbContextOptions<MessengerDBContext> options) : base(options) {}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder
            .UseSnakeCaseNamingConvention();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}