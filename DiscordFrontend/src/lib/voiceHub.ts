import * as signalR from "@microsoft/signalr";

import { API_URL } from "@/lib/api";

export interface VoiceParticipant {
  connectionId: string;
  userId: number;
  username: string;
  displayName: string;
}

export function createVoiceHubConnection(
  accessToken: string
): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(
      `${API_URL}/hubs/voice`,
      {
        accessTokenFactory: () =>
          accessToken,
      }
    )
    .withAutomaticReconnect()
    .configureLogging(
      signalR.LogLevel.Information
    )
    .build();
}