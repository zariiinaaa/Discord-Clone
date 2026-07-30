using System;
using System.Collections.Generic;
using System.Text;
using Discord.Core.Enums;

namespace Discord.Core.DTOs.Friends.Responses
{
    public class FriendRequestResponseDto
    {
        public int Id { get; set; }
        public FriendUserResponseDto Sender { get; set; }= null!;
        public FriendUserResponseDto Receiver { get; set; } = null!;
        public FriendRequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}