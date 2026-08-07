using Discord.Core.Entities.Common;
using Discord.Core.Entities.Conversations;
using Discord.Core.Entities.Friends;
using Discord.Core.Entities.Messages;
using Discord.Core.Entities.Privacy;
using Discord.Core.Entities.Servers;
using Discord.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Entities
{
    public class User:BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? EmailVerifiedAt { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Offline;
        public UserStatus PreferredStatus { get; set; } = UserStatus.Online;
        public PlatformRole Role { get; set; } = PlatformRole.User;
        public DateTime? LastSeenAt { get; set; }
        public bool IsBanned { get; set; }
        public string? BanReason { get; set; }
        public DateTime? BannedUntil { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<AuthOneTimeToken> AuthOneTimeTokens { get; set; } = new List<AuthOneTimeToken>();
        public ICollection<Server> OwnedServers { get; set; }= new List<Server>();
        public ICollection<ServerMember> ServerMemberships { get; set; }= new List<ServerMember>();
        public ICollection<ServerInvite> CreatedServerInvites { get; set; } = new List<ServerInvite>();
        public ICollection<Message> SentMessages { get; set; }= new List<Message>();
        public ICollection<FriendRequest> SentFriendRequests { get; set; }= new List<FriendRequest>();
        public ICollection<FriendRequest> ReceivedFriendRequests { get; set; } = new List<FriendRequest>();
        public ICollection<Friendship> FriendshipsAsUser { get; set; }= new List<Friendship>();
        public ICollection<Friendship> FriendshipsAsFriend { get; set; } = new List<Friendship>();
        public ICollection<UserBlock> BlockedUsers { get; set; } = new List<UserBlock>();
        public ICollection<UserBlock> BlockedByUsers { get; set; }= new List<UserBlock>();
        public ICollection<ConversationMember>ConversationMemberships{ get; set; }= new List<ConversationMember>();
        public ICollection<Conversation> OwnedConversations{ get; set; }= new List<Conversation>();

        public UserPrivacySettings? PrivacySettings{get;set;}
        public ICollection<DirectMessageRequest>SentDirectMessageRequests { get; set; } = new List<DirectMessageRequest>();
        public ICollection<DirectMessageRequest>ReceivedDirectMessageRequests { get; set; }= new List<DirectMessageRequest>();
        public ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();

       

    }
}
