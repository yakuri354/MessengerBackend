using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MessengerBackend.Models
{
    public class MessengerDBContext : DbContext
    {
        public DbSet<User> Users;
        public DbSet<Bot> Bots;
        public DbSet<Room> Rooms;
        public DbSet<Message> Messages;
        public DbSet<Session> Sessions;
        


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSnakeCaseNamingConvention();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<RoomType>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<SessionType>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<SessionType>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasPostgresEnum<RoomType>()
                .HasPostgresEnum<SessionType>()
                .HasPostgresEnum<SessionPlatform>();
            modelBuilder.Entity<User>().Property(p => p.ID).HasIdentityOptions();
            modelBuilder.Entity<Session>().Property(p => p.ID).HasIdentityOptions();
            modelBuilder.Entity<Message>().Property(p => p.ID).HasIdentityOptions();
            modelBuilder.Entity<Room>().Property(p => p.ID).HasIdentityOptions();
            modelBuilder.Entity<Bot>().Property(p => p.ID).HasIdentityOptions();
        }
    }
}