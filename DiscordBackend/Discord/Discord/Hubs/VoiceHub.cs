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

    private readonly VoiceConnectionTracker _connectionTracker;

    public VoiceHub(IChannelAccessService channelAccessService,VoiceConnectionTracker connectionTracker)
    {
        _channelAccessService = channelAccessService;
        _connectionTracker = connectionTracker;
    }

    public async Task JoinVoiceChannel(int channelId)
    {
        var userId = GetCurrentUserId();

        await _channelAccessService
            .GetAccessibleVoiceChannelAsync(
                channelId,
                userId,
                Context.ConnectionAborted);

        var existingConnection =
            _connectionTracker.GetConnection(
                Context.ConnectionId);

        if (existingConnection is not null)
        {
            if (existingConnection.ChannelId == channelId)
            {
                await BroadcastParticipantsAsync(
                    channelId);

                return;
            }

            _connectionTracker.RemoveConnection(
                Context.ConnectionId);

            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                GetVoiceGroupName(
                    existingConnection.ChannelId),
                Context.ConnectionAborted);

            await BroadcastParticipantsAsync(
                existingConnection.ChannelId);
        }

        var username =
            Context.User?.FindFirstValue(
                ClaimTypes.Name)
            ?? "Unknown";

        var displayName =
            Context.User?.FindFirstValue(
                "display_name")
            ?? username;

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

    public async Task LeaveVoiceChannel(int channelId)
    {
        var connection =_connectionTracker.GetConnection( Context.ConnectionId);

        if (connection is null ||
            connection.ChannelId != channelId)
        {
            return;
        }

        _connectionTracker.RemoveConnection(
            Context.ConnectionId);

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GetVoiceGroupName(channelId),
            Context.ConnectionAborted);

        await BroadcastParticipantsAsync(channelId);
    }

    public async Task SendWebRtcOffer(string targetConnectionId, string offerJson)
    {
        EnsureValidSignalPayload(
            offerJson,
            "WebRTC offer");

        EnsureCanSendSignal(targetConnectionId);

        await Clients
            .Client(targetConnectionId)
            .ReceiveWebRtcOffer(
                Context.ConnectionId,
                offerJson);
    }

    public async Task SendWebRtcAnswer(string targetConnectionId,string answerJson)
    {
        EnsureValidSignalPayload(
            answerJson,
            "WebRTC answer");

        EnsureCanSendSignal(targetConnectionId);

        await Clients
            .Client(targetConnectionId)
            .ReceiveWebRtcAnswer(
                Context.ConnectionId,
                answerJson);
    }

    public async Task SendIceCandidate(string targetConnectionId,string candidateJson)
    {
        EnsureValidSignalPayload(
            candidateJson,
            "ICE candidate");

        EnsureCanSendSignal(targetConnectionId);

        await Clients
            .Client(targetConnectionId)
            .ReceiveIceCandidate(
                Context.ConnectionId,
                candidateJson);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connection = _connectionTracker.RemoveConnection(Context.ConnectionId);

        if (connection is not null)
        {
            await BroadcastParticipantsAsync(
                connection.ChannelId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private void EnsureCanSendSignal(string targetConnectionId)
    {
        if (string.IsNullOrWhiteSpace(
            targetConnectionId))
        {
            throw new HubException(
                "Hədəf bağlantı düzgün deyil.");
        }

        if (targetConnectionId ==
            Context.ConnectionId)
        {
            throw new HubException(
                "İstifadəçi öz bağlantısına siqnal göndərə bilməz.");
        }

        var senderConnection =
            _connectionTracker.GetConnection(
                Context.ConnectionId)
            ?? throw new HubException(
                "Əvvəlcə voice kanalına qoşulmalısınız.");

        var targetConnection =
            _connectionTracker.GetConnection(
                targetConnectionId)
            ?? throw new HubException(
                "Hədəf istifadəçi voice kanalında deyil.");

        if (senderConnection.ChannelId !=
            targetConnection.ChannelId)
        {
            throw new HubException(
                "İstifadəçilər eyni voice kanalında deyil.");
        }
    }

    private static void EnsureValidSignalPayload(string payload,string payloadName)
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
        var participants =_connectionTracker.GetParticipants(channelId);

        await Clients
            .Group(GetVoiceGroupName(channelId))
            .VoiceParticipantsUpdated(
                channelId,
                participants);
    }

    private int GetCurrentUserId()
    {
        var userIdValue =Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse( userIdValue,out var userId))
        {
            throw new HubException("Access token etibarsızdır.");
        }

        return userId;
    }

    public static string GetVoiceGroupName(int channelId)
    {
        return $"voice:{channelId}";
    }
}