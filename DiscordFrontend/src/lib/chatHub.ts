"use client";

import * as signalR from "@microsoft/signalr";

const API_URL =
  process.env.NEXT_PUBLIC_API_URL ??
  "http://localhost:5151";

const wait = (
  milliseconds: number
): Promise<void> => {
  return new Promise(resolve => {
    window.setTimeout(
      resolve,
      milliseconds
    );
  });
};

export const createChatHubConnection = (
  accessToken: string
): signalR.HubConnection => {
  return new signalR.HubConnectionBuilder()
    .withUrl(
      `${API_URL}/hubs/chat`,
      {
        accessTokenFactory: () =>
          accessToken,
      }
    )
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds:
        retryContext => {
          if (
            retryContext
              .elapsedMilliseconds <
            60_000
          ) {
            return 2_000;
          }

          return 5_000;
        },
    })
    .configureLogging(
      signalR.LogLevel.Information
    )
    .build();
};

export async function startChatHubConnection(
  connection: signalR.HubConnection,
  isCancelled: () => boolean
): Promise<boolean> {
  /*
   * React development mode-un ilk effect
   * cleanup əməliyyatını tamamlamasını gözləyir.
   * Beləliklə connection negotiation zamanı
   * lazımsız şəkildə dayandırılmır.
   */
  await wait(0);

  if (isCancelled()) {
    return false;
  }

  let retryAttempt = 0;

  while (!isCancelled()) {
    if (
      connection.state ===
      signalR.HubConnectionState.Connected
    ) {
      return true;
    }

    if (
      connection.state ===
        signalR.HubConnectionState.Connecting ||
      connection.state ===
        signalR.HubConnectionState.Reconnecting ||
      connection.state ===
        signalR.HubConnectionState.Disconnecting
    ) {
      await wait(250);
      continue;
    }

    try {
      await connection.start();

      return true;
    } catch (connectionError) {
      if (isCancelled()) {
        return false;
      }

      retryAttempt += 1;

      const retryDelay =
        Math.min(
          retryAttempt * 1_000,
          5_000
        );

      console.error(
        `ChatHub initial connection failed. Retrying in ${retryDelay}ms:`,
        connectionError
      );

      await wait(retryDelay);
    }
  }

  return false;
}