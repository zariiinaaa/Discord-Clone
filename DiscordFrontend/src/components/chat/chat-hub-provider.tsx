"use client";

import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";

import type {
  PropsWithChildren,
} from "react";

import {
  HubConnectionState,
} from "@microsoft/signalr";

import type {
  HubConnection,
} from "@microsoft/signalr";

import {
  createChatHubConnection,
  startChatHubConnection,
} from "@/lib/chatHub";

import { useAuthStore } from "@/state/auth";

export type ChatHubStatus =
  | "disconnected"
  | "connecting"
  | "connected"
  | "reconnecting";

interface ChatHubContextValue {
  connection: HubConnection | null;
  status: ChatHubStatus;
  isConnected: boolean;
}

const ChatHubContext =
  createContext<ChatHubContextValue | null>(
    null
  );

/*
 * Backend-in göndərə bildiyi bütün ChatHub
 * event-ləri burada əvvəlcədən qeyd olunur.
 *
 * Beləliklə hansı səhifənin açıq olmasından
 * asılı olmayaraq SignalR həmin event-i
 * tanıyır və terminal warning vermir.
 */
const CHAT_HUB_EVENTS = [
  "MessageCreated",
  "MessageUpdated",
  "MessageDeleted",

  "ConversationMessageCreated",
  "ConversationMessageUpdated",
  "ConversationMessageDeleted",
  "ConversationTypingChanged",

  "DirectMessageRequestCreated",
  "ConversationDataChanged",
  "FriendDataChanged",
] as const;

export function ChatHubProvider({
  children,
}: PropsWithChildren) {
  const accessToken = useAuthStore(
    state => state.accessToken
  );

  const [connection, setConnection] =
    useState<HubConnection | null>(null);

  const [status, setStatus] =
    useState<ChatHubStatus>(
      "disconnected"
    );

  useEffect(() => {
    if (!accessToken) {
      setConnection(null);
      setStatus("disconnected");
      return;
    }

    let isDisposed = false;

    const newConnection =
      createChatHubConnection(
        accessToken
      );

    /*
     * Bu boş handler-lər event warning-lərinin
     * qarşısını alır. Səhifələr daha sonra eyni
     * connection-a öz real handler-lərini əlavə
     * edəcəklər.
     */
    const passiveEventHandler =
      () => undefined;

    /*
     * Conversation siyahısının refreshsiz
     * yenilənməsini təmin edir.
     */
    const handleConversationDataChanged =
      () => {
        console.log(
          "ConversationDataChanged received"
        );

        window.dispatchEvent(
          new Event(
            "conversations:refresh"
          )
        );
      };

    /*
     * Dost siyahısı və friend request
     * məlumatlarının refreshsiz yenilənməsini
     * təmin edir.
     */
    const handleFriendDataChanged =
      () => {
        console.log(
          "FriendDataChanged received"
        );

        window.dispatchEvent(
          new Event(
            "friends:refresh"
          )
        );
      };

    CHAT_HUB_EVENTS.forEach(
      eventName => {
        newConnection.on(
          eventName,
          passiveEventHandler
        );
      }
    );

    newConnection.on(
      "FriendDataChanged",
      handleFriendDataChanged
    );

    newConnection.on(
      "ConversationDataChanged",
      handleConversationDataChanged
    );

    newConnection.onreconnecting(() => {
      if (!isDisposed) {
        setStatus("reconnecting");
      }
    });

    newConnection.onreconnected(() => {
      if (!isDisposed) {
        setStatus("connected");
      }
    });

    newConnection.onclose(() => {
      if (!isDisposed) {
        setStatus("disconnected");
      }
    });

    /*
     * Connection start edilməzdən əvvəl
     * context vasitəsilə child component-lərə
     * verilir. Beləliklə onlar öz handler-lərini
     * connection-a əlavə edə bilirlər.
     */
    setConnection(newConnection);

    const startConnection = async () => {
      try {
        setStatus("connecting");

        const connectionStarted =
          await startChatHubConnection(
            newConnection,
            () => isDisposed
          );

        if (
          !connectionStarted ||
          isDisposed
        ) {
          return;
        }

        setStatus("connected");
      } catch (connectionError) {
        if (!isDisposed) {
          console.error(
            "Global ChatHub connection failed:",
            connectionError
          );

          setStatus("disconnected");
        }
      }
    };

    void startConnection();

    return () => {
      isDisposed = true;

      newConnection.off(
        "ConversationDataChanged",
        handleConversationDataChanged
      );

      newConnection.off(
        "FriendDataChanged",
        handleFriendDataChanged
      );

      CHAT_HUB_EVENTS.forEach(
        eventName => {
          newConnection.off(
            eventName,
            passiveEventHandler
          );
        }
      );

      void newConnection
        .stop()
        .catch(() => undefined);
    };
  }, [accessToken]);

  const contextValue =
    useMemo<ChatHubContextValue>(
      () => ({
        connection,
        status,

        isConnected:
          status === "connected" &&
          connection?.state ===
            HubConnectionState.Connected,
      }),
      [
        connection,
        status,
      ]
    );

  return (
    <ChatHubContext.Provider
      value={contextValue}
    >
      {children}
    </ChatHubContext.Provider>
  );
}

export function useChatHub() {
  const context =
    useContext(ChatHubContext);

  if (!context) {
    throw new Error(
      "useChatHub must be used inside ChatHubProvider."
    );
  }

  return context;
}