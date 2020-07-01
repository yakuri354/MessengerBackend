using MessengerBackend.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MessengerBackend.Services
{
    public class MessengerDBContext : DbContext
    {
        public DbSet<Bot> Bots;
        public DbSet<Message> Messages;
        public DbSet<Room> Rooms;
        public DbSet<Session> Sessions;
        public DbSet<User> Users;

        public MessengerDBContext(DbContextOptions<MessengerDBContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSnakeCaseNamingConvention();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<RoomType>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasPostgresEnum<RoomType>();
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Session>().ToTable("sessions");
            modelBuilder.Entity<Message>().ToTable("messages");
            modelBuilder.Entity<Room>().ToTable("rooms");
            modelBuilder.Entity<Bot>().ToTable("bots");
            modelBuilder.UseHiLo();
        }
    }
}