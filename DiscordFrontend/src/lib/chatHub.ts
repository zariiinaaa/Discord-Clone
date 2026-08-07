"use client";

import * as signalR from "@microsoft/signalr";

import {
  getRefreshedAccessToken,
} from "@/lib/api";

import { useAuthStore } from "@/state/auth";

const API_URL = (
  process.env.NEXT_PUBLIC_API_URL ??
  "http://localhost:5151"
).replace(/\/$/, "");

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

function isAccessTokenExpiring(
  accessToken: string
): boolean {
  try {
    const payloadPart =
      accessToken.split(".")[1];

    if (!payloadPart) {
      return true;
    }

    const normalizedPayload =
      payloadPart
        .replace(/-/g, "+")
        .replace(/_/g, "/");

    const paddedPayload =
      normalizedPayload.padEnd(
        Math.ceil(
          normalizedPayload.length / 4
        ) * 4,
        "="
      );

    const payload = JSON.parse(
      window.atob(paddedPayload)
    ) as {
      exp?: number;
    };

    if (
      typeof payload.exp !== "number"
    ) {
      return true;
    }

    /*
     * Tokenin bitməsinə 30 saniyədən
     * az qalıbsa əvvəlcədən refresh edir.
     */
    return (
      payload.exp * 1000 <=
      Date.now() + 30_000
    );
  } catch {
    return true;
  }
}

async function getCurrentAccessToken():
  Promise<string> {
  const accessToken =
    useAuthStore.getState().accessToken;

  if (!accessToken) {
    return "";
  }

  if (
    !isAccessTokenExpiring(
      accessToken
    )
  ) {
    return accessToken;
  }

  return getRefreshedAccessToken();
}

function isUnauthorizedError(
  error: unknown
): boolean {
  const errorMessage =
    String(error).toLowerCase();

  return (
    errorMessage.includes(
      "unauthorized"
    ) ||
    errorMessage.includes(
      "status code '401'"
    ) ||
    errorMessage.includes(
      "status code 401"
    )
  );
}

export const createChatHubConnection = (
  accessTokenOverride?: string
): signalR.HubConnection => {
    return new signalR.HubConnectionBuilder()
      .withUrl(
        `${API_URL}/hubs/chat`,
        {
         
        accessTokenFactory:
  accessTokenOverride
    ? () => accessTokenOverride
    : getCurrentAccessToken,
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
   * cleanup əməliyyatını tamamlamasını
   * gözləyir.
   */
  await wait(0);

  if (isCancelled()) {
    return false;
  }

  let retryAttempt = 0;
  let unauthorizedRetryUsed = false;

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

      if (
        isUnauthorizedError(
          connectionError
        )
      ) {
        /*
         * Eyni connection üçün yalnız bir
         * dəfə token refresh etməyə çalışır.
         * Beləliklə sonsuz 401 retry yaranmır.
         */
        if (unauthorizedRetryUsed) {
          return false;
        }

        unauthorizedRetryUsed = true;

        try {
          await getRefreshedAccessToken();
        } catch {
          return false;
        }

        retryAttempt = 0;
        continue;
      }

      retryAttempt += 1;

      const retryDelay = Math.min(
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