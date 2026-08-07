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
        public DbSet<AuthOneTimeToken> AuthOneTimeTokens => Set<AuthOneTimeToken>();
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
        public DbSet<MessageAttachment> MessageAttachments { get; set; }= null!;
        public DbSet<MessageReaction> MessageReactions =>Set<MessageReaction>();
        public DbSet<ServerRole> ServerRoles =>Set<ServerRole>();
        public DbSet<ServerRolePermission> ServerRolePermissions =>Set<ServerRolePermission>();

        public DbSet<ServerMemberRole> ServerMemberRoles =>Set<ServerMemberRole>();
        public DbSet<ChannelRolePermissionOverride> ChannelRolePermissionOverrides
                => Set<ChannelRolePermissionOverride>();

        public DbSet<ChannelMemberPermissionOverride> ChannelMemberPermissionOverrides
                    => Set<ChannelMemberPermissionOverride>();
        public DbSet<ServerBan> ServerBans => Set<ServerBan>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
