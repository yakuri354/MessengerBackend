using System;
using MessengerBackend.Database.MessengerBackend.Database.ValueGenerators;
using MessengerBackend.Models;
using MessengerBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Npgsql;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace MessengerBackend.Database
{
    public class MessengerDBContext : DbContext
    {
        public MessengerDBContext(DbContextOptions<MessengerDBContext> options
            // , ILogger<MessengerDBContext> logger
        ) : base(options)
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
                .HasPostgresEnum<RoomType>()
                .HasPostgresEnum<SubscriptionType>()
                .HasPostgresEnum<ParticipantRole>();
            modelBuilder.Entity<User>(ub =>
            {
                ub.ToTable("users");
                ub.HasMany(u => u.RoomsParticipants)
                    .WithOne(p => p.User)
                    .HasForeignKey(p => p.UserID);
                ub.HasIndex(u => u.Number).IsUnique();
                ub.HasIndex(u => u.Username).IsUnique();
                ub.HasIndex(u => u.UserPID).IsUnique();
                ub.Property(u => u.UserPID).ValueGeneratedOnAdd().HasValueGenerator<PIDGenerator>();
                ub.Property(u => u.JoinedAt).ValueGeneratedOnAdd().HasValueGenerator<NowGenerator>();
            });
            modelBuilder.Entity<Session>(sb =>
            {
                sb.ToTable("sessions").Property(s => s.CreatedAt).ValueGeneratedOnAdd()
                    .HasValueGenerator<NowGenerator>();
                sb.Property(s => s.RefreshToken).ValueGeneratedOnAdd()
                    .HasValueGenerator<RefreshTokenGenerator>();
                sb.HasOne(s => s.User)
                    .WithMany(u => u.Sessions)
                    .HasForeignKey(s => s.UserID);
            });
            modelBuilder.Entity<Message>(mb =>
            {
                mb.ToTable("messages");
                mb.Property(m => m.SentAt).ValueGeneratedOnAdd()
                    .HasValueGenerator<NowGenerator>();
                mb.Property(m => m.MessagePID).ValueGeneratedOnAdd()
                    .HasValueGenerator<PIDGenerator>();
                mb.HasOne(m => m.TargetRoom)
                    .WithMany(r => r.Messages)
                    .HasForeignKey(m => m.TargetRoomID);
                mb.Property(m => m.TargetRoomID).IsRequired();
                mb.HasIndex(m => m.TargetRoomID);
                mb.HasIndex(m => m.MessagePID).IsUnique();
            });
            modelBuilder.Entity<Room>(rb =>
            {
                rb.ToTable("rooms");
                rb.HasMany(r => r.Participants)
                    .WithOne(p => p.Room)
                    .HasForeignKey(p => p.RoomID);
                rb.HasIndex(r => r.RoomPID).IsUnique();
                rb.HasIndex(r => r.Link).IsUnique();
                rb.Property(r => r.RoomPID).ValueGeneratedOnAdd().HasValueGenerator<PIDGenerator>();
                rb.HasIndex(r => r.RoomPID).IsUnique();
                rb.Property(r => r.CreatedAt).ValueGeneratedOnAdd()
                    .HasValueGenerator<NowGenerator>();
            });
            // modelBuilder.Entity<Bot>(b =>
            // {
            //     b.ToTable("bots").HasAlternateKey(e => e.BotUsername);
            //     b.Property(p => p.JoinedAt).ValueGeneratedOnAdd().HasValueGenerator<NowGenerator>();
            // });

            // EF Core does not support many to many (in 3.0) so this is how I implemented it
            modelBuilder.Entity<RoomParticipant>(rpb =>
            {
                rpb.HasKey(rp => new { rp.RoomID, rp.UserID });
                rpb.HasOne(rp => rp.Room)
                    .WithMany(r => r.Participants)
                    .HasForeignKey(rp => rp.RoomID);
                rpb.HasOne(rp => rp.User)
                    .WithMany(u => u.RoomsParticipants)
                    .HasForeignKey(rp => rp.UserID);
            });
            modelBuilder.Entity<Subscription>(sb =>
            {
                sb.HasMany(sbp => sbp.Events)
                    .WithOne(e => e.Subscription)
                    .HasForeignKey(e => e.SubscriptionID);
                sb.Property(sbp => sbp.SubscribedAt)
                    .ValueGeneratedOnAdd()
                    .HasValueGenerator<NowGenerator>();
                sb.Property(sbp => sbp.SubscriptionPID)
                    .ValueGeneratedOnAdd()
                    .HasValueGenerator<PIDGenerator>();
                sb.HasIndex(s => s.SubscriptionPID).IsUnique();
            });
            modelBuilder.Entity<Event>(eb =>
            {
                eb.Property(e => e.OccuredAt)
                    .ValueGeneratedOnAdd().HasValueGenerator<NowGenerator>();
                eb.Property(e => e.EventPID)
                    .ValueGeneratedOnAdd().HasValueGenerator<PIDGenerator>();
                eb.HasIndex(e => e.EventPID).IsUnique();
            });
            modelBuilder.UseHiLo();
        }
#nullable disable
        // public DbSet<Bot> Bots { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RoomParticipant> RoomParticipants { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Event> Events { get; set; }
#nullable restore
    }

    namespace MessengerBackend.Database.ValueGenerators
    {
        public class PIDGenerator : ValueGenerator
        {
            public override bool GeneratesTemporaryValues => false;

            protected override object NextValue(EntityEntry entry) => entry.Entity switch
            {
                User _ => CryptoService.GeneratePID("U"),
                Room _ => CryptoService.GeneratePID("R"),
                Message _ => CryptoService.GeneratePID("M"),
                Subscription _ => CryptoService.GeneratePID("S"),
                Event _ => CryptoService.GeneratePID("E"),
                // Bot _ => CryptoService.GeneratePID("B"),
                _ => throw new ArgumentOutOfRangeException(
                    $"No PID generation available for {entry.Entity.GetType()}")
            };
        }

        public class RefreshTokenGenerator : ValueGenerator
        {
            public override bool GeneratesTemporaryValues => false;

            protected override object NextValue(EntityEntry entry) => CryptoService.GenerateRefreshToken();
        }

        public class NowGenerator : ValueGenerator
        {
            public override bool GeneratesTemporaryValues => false;
            protected override object NextValue(EntityEntry entry) => DateTime.UtcNow;
        }
    }
}