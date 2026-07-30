using Discord.Core.Entities;
using Discord.Core.Entities.Conversations;
using Discord.Core.Entities.Friends;
using Discord.Core.Entities.Messages;
using Discord.Core.Entities.Privacy;
using Discord.Core.Entities.Servers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Infrastructure.Data
{
    public class AppDbContext: DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options): base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Server> Servers => Set<Server>();
        public DbSet<ServerMember> ServerMembers =>Set<ServerMember>();
       public DbSet<Channel> Channels => Set<Channel>();
        public DbSet<ServerInvite> ServerInvites =>Set<ServerInvite>();
        public DbSet<Message> Messages =>Set<Message>();
        public DbSet<FriendRequest> FriendRequests =>Set<FriendRequest>();
        public DbSet<Friendship> Friendships =>Set<Friendship>();
        public DbSet<UserBlock> UserBlocks =>Set<UserBlock>();
        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<ConversationMember> ConversationMembers=> Set<ConversationMember>();
        public DbSet<UserPrivacySettings>UserPrivacySettings =>Set<UserPrivacySettings>();
        public DbSet<DirectMessageRequest>DirectMessageRequests =>Set<DirectMessageRequest>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
