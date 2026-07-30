namespace Discord.Hubs;

public interface IVoiceClient
{
    Task VoiceParticipantsUpdated(int channelId,IReadOnlyCollection<VoiceParticipant> participants);

    Task ReceiveWebRtcOffer(string senderConnectionId,string offerJson);

    Task ReceiveWebRtcAnswer(string senderConnectionId,string answerJson);

    Task ReceiveIceCandidate(string senderConnectionId,string candidateJson);
}