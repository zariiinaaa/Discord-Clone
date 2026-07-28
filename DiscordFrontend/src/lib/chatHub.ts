"use client";

import * as signalR from "@microsoft/signalr";

const API_URL =
  process.env.NEXT_PUBLIC_API_URL ??
  "http://localhost:5151";

export const createChatHubConnection = (
  accessToken: string
): signalR.HubConnection => {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${API_URL}/hubs/chat`, {
      accessTokenFactory: () => accessToken
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();
};