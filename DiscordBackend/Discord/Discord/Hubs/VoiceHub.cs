using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Discord.Hubs;

[Authorize]
public class VoiceHub : Hub<IVoiceClient>
{
    private readonly IChannelAccessService _channelAccessService;
    private readonly IChannelPermissionService _channelPermissionService;
    private readonly IConversationAccessService _conversationAccessService;
    private readonly VoiceConnectionTracker _connectionTracker;

    public VoiceHub(
        IChannelAccessService channelAccessService,
        IChannelPermissionService channelPermissionService,
        IConversationAccessService conversationAccessService,
        VoiceConnectionTracker connectionTracker)
    {
        _channelAccessService = channelAccessService;
        _channelPermissionService = channelPermissionService;
        _conversationAccessService = conversationAccessService;
        _connectionTracker = connectionTracker;
    }

    public async Task JoinVoiceChannel(int channelId)
    {
        var userId = GetCurrentUserId();

        await _channelAccessService.GetAccessibleVoiceChannelAsync(
            channelId, userId, Context.ConnectionAborted);

        try
        {
            await _channelPermissionService.EnsurePermissionAsync(
                channelId, userId, ServerPermission.Connect,
                Context.ConnectionAborted);
        }
        catch (ForbiddenException)
        {
            throw new HubException(
                "Bu voice kanalına qoşulmaq icazəniz yoxdur.");
        }

        var existingConnection = _connectionTracker.GetConnection(
            Context.ConnectionId);

        if (existingConnection is not null)
        {
            var isSameChannel =
                existingConnection.ConversationId is null &&
                existingConnection.ChannelId == channelId;

            if (isSameChannel)
            {
                await BroadcastParticipantsAsync(channelId);
                return;
            }

            await RemoveFromCurrentRoomAsync(existingConnection);
        }

        var username = Context.User?.FindFirstValue(
            ClaimTypes.Name) ?? "Unknown";

        var displayName = Context.User?.FindFirstValue(
            "display_name") ?? username;

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetVoiceGroupName(channelId),
            Context.ConnectionAborted);

        _connectionTracker.AddConnection(
            Context.ConnectionId,
            channelId,
            userId,
            username,
            displayName);

        await BroadcastParticipantsAsync(channelId);
    }

    public async Task<bool> CanSpeakInVoiceChannel(int channelId)
    {
        var userId = GetCurrentUserId();

        await _channelAccessService.GetAccessibleVoiceChannelAsync(
            channelId, userId, Context.ConnectionAborted);

        try
        {
            await _channelPermissionService.EnsurePermissionAsync(
                channelId, userId, ServerPermission.Connect,
                Context.ConnectionAborted);
        }
        catch (ForbiddenException)
        {
            throw new HubException(
                "Bu voice kanalına qoşulmaq icazəniz yoxdur.");
        }

        try
        {
            await _channelPermissionService.EnsurePermissionAsync(
                channelId, userId, ServerPermission.Speak,
                Context.ConnectionAborted);

            return true;
        }
        catch (ForbiddenException)
        {
            return false;
        }
    }

    public async Task JoinConversationVoice(int conversationId)
    {
        var userId = GetCurrentUserId();

        await _conversationAccessService.GetAccessibleConversationAsync(
            conversationId, userId, Context.ConnectionAborted);

        var existingConnection = _connectionTracker.GetConnection(
            Context.ConnectionId);

        if (existingConnection is not null)
        {
            if (existingConnection.ConversationId == conversationId)
            {
                await BroadcastConversationParticipantsAsync(conversationId);
                return;
            }

            await RemoveFromCurrentRoomAsync(existingConnection);
        }

        var username = Context.User?.FindFirstValue(
            ClaimTypes.Name) ?? "Unknown";

        var displayName = Context.User?.FindFirstValue(
            "display_name") ?? username;

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetConversationVoiceGroupName(conversationId),
            Context.ConnectionAborted);

        _connectionTracker.AddConversationConnection(
            Context.ConnectionId,
            conversationId,
            userId,
            username,
            displayName);

        await BroadcastConversationParticipantsAsync(conversationId);
    }

    public async Task LeaveVoiceChannel(int channelId)
    {
        var connection = _connectionTracker.GetConnection(
            Context.ConnectionId);

        if (connection is null ||
            connection.ConversationId is not null ||
            connection.ChannelId != channelId)
        {
            return;
        }

        _connectionTracker.RemoveConnection(Context.ConnectionId);

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GetVoiceGroupName(channelId),
            Context.ConnectionAborted);

        await BroadcastParticipantsAsync(channelId);
    }

    public async Task LeaveConversationVoice(int conversationId)
    {
        var connection = _connectionTracker.GetConnection(
            Context.ConnectionId);

        if (connection is null ||
            connection.ConversationId != conversationId)
        {
            return;
        }

        _connectionTracker.RemoveConnection(Context.ConnectionId);

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GetConversationVoiceGroupName(conversationId),
            Context.ConnectionAborted);

        await BroadcastConversationParticipantsAsync(conversationId);
    }

    public async Task SendWebRtcOffer(
        string targetConnectionId,
        string offerJson)
    {
        EnsureValidSignalPayload(offerJson, "WebRTC offer");
        EnsureCanSendSignal(targetConnectionId);

        await Clients.Client(targetConnectionId).ReceiveWebRtcOffer(
            Context.ConnectionId, offerJson);
    }

    public async Task SendWebRtcAnswer(
        string targetConnectionId,
        string answerJson)
    {
        EnsureValidSignalPayload(answerJson, "WebRTC answer");
        EnsureCanSendSignal(targetConnectionId);

        await Clients.Client(targetConnectionId).ReceiveWebRtcAnswer(
            Context.ConnectionId, answerJson);
    }

    public async Task SendIceCandidate(
        string targetConnectionId,
        string candidateJson)
    {
        EnsureValidSignalPayload(candidateJson, "ICE candidate");
        EnsureCanSendSignal(targetConnectionId);

        await Clients.Client(targetConnectionId).ReceiveIceCandidate(
            Context.ConnectionId, candidateJson);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connection = _connectionTracker.RemoveConnection(
            Context.ConnectionId);

        if (connection is not null)
        {
            await BroadcastConnectionRoomAsync(connection);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private void EnsureCanSendSignal(string targetConnectionId)
    {
        if (string.IsNullOrWhiteSpace(targetConnectionId))
        {
            throw new HubException("Hədəf bağlantı düzgün deyil.");
        }

        if (targetConnectionId == Context.ConnectionId)
        {
            throw new HubException(
                "İstifadəçi öz bağlantısına siqnal göndərə bilməz.");
        }

        _ = _connectionTracker.GetConnection(Context.ConnectionId)
            ?? throw new HubException(
                "Əvvəlcə voice otağına qoşulmalısınız.");

        _ = _connectionTracker.GetConnection(targetConnectionId)
            ?? throw new HubException(
                "Hədəf istifadəçi voice otağında deyil.");

        if (!_connectionTracker.AreInSameRoom(
            Context.ConnectionId, targetConnectionId))
        {
            throw new HubException(
                "İstifadəçilər eyni voice otağında deyil.");
        }
    }

    private async Task RemoveFromCurrentRoomAsync(
        VoiceConnectionInfo connection)
    {
        _connectionTracker.RemoveConnection(connection.ConnectionId);

        if (connection.ConversationId.HasValue)
        {
            var conversationId = connection.ConversationId.Value;

            await Groups.RemoveFromGroupAsync(
                connection.ConnectionId,
                GetConversationVoiceGroupName(conversationId),
                Context.ConnectionAborted);

            await BroadcastConversationParticipantsAsync(conversationId);
            return;
        }

        await Groups.RemoveFromGroupAsync(
            connection.ConnectionId,
            GetVoiceGroupName(connection.ChannelId),
            Context.ConnectionAborted);

        await BroadcastParticipantsAsync(connection.ChannelId);
    }

    private async Task BroadcastConnectionRoomAsync(
        VoiceConnectionInfo connection)
    {
        if (connection.ConversationId.HasValue)
        {
            await BroadcastConversationParticipantsAsync(
                connection.ConversationId.Value);

            return;
        }

        await BroadcastParticipantsAsync(connection.ChannelId);
    }

    private static void EnsureValidSignalPayload(
        string payload,
        string payloadName)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new HubException(
                $"{payloadName} boş ola bilməz.");
        }

        if (payload.Length > 100_000)
        {
            throw new HubException(
                $"{payloadName} həddindən artıq böyükdür.");
        }
    }

    private async Task BroadcastParticipantsAsync(int channelId)
    {
        var participants = _connectionTracker.GetParticipants(channelId);

        await Clients.Group(GetVoiceGroupName(channelId))
            .VoiceParticipantsUpdated(channelId, participants);
    }

    private async Task BroadcastConversationParticipantsAsync(
        int conversationId)
    {
        var participants = _connectionTracker
            .GetConversationParticipants(conversationId);

        await Clients.Group(GetConversationVoiceGroupName(conversationId))
            .VoiceParticipantsUpdated(
                GetConversationRoomId(conversationId),
                participants);
    }

    private int GetCurrentUserId()
    {
        var userIdValue = Context.User?.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new HubException("Access token etibarsızdır.");
        }

        return userId;
    }

    private static int GetConversationRoomId(int conversationId)
    {
        return -conversationId;
    }

    public static string GetVoiceGroupName(int channelId)
    {
        return $"voice:{channelId}";
    }

    public static string GetConversationVoiceGroupName(int conversationId)
    {
        return $"voice-conversation:{conversationId}";
    }
}