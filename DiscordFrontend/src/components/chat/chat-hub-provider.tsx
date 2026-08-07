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

import { useCurrentUserStore } from "@/state/user";

import {
  StaticUserStatuses,
} from "@/lib/entities/user";

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
  "UserPresenceChanged",
    "MessageReactionChanged",
  "ConversationMessageReactionChanged",
  "ServerMembersChanged",
  "ServerMemberRemoved",
  "ServerChannelsChanged",
  "ChannelAccessRevoked",
] as const;

export function ChatHubProvider({
  children,
}: PropsWithChildren) {
  const accessToken = useAuthStore(
    state => state.accessToken
  );
  const hasAccessToken =
  Boolean(accessToken);

  const [connection, setConnection] =
    useState<HubConnection | null>(null);

  const [status, setStatus] =
    useState<ChatHubStatus>(
      "disconnected"
    );

  useEffect(() => {
    if (!hasAccessToken) {
      setConnection(null);
      setStatus("disconnected");
      return;
    }

    let isDisposed = false;

    const newConnection =
  createChatHubConnection();

  
    const passiveEventHandler =
      () => undefined;

   
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

  const handleUserPresenceChanged = (
  userId: number,
  status: number
) => {
  window.dispatchEvent(
    new CustomEvent(
      "user-presence:changed",
      {
        detail: { userId, status },
      }
    )
  );

  

  const frontendStatus =
    status === 1
      ? StaticUserStatuses.Online
      : status === 2
        ? StaticUserStatuses.Idle
        : status === 3
          ? StaticUserStatuses.DND
          : StaticUserStatuses.Offline;

  const {
    currentUser,
    setCurrentUser,
  } = useCurrentUserStore.getState();

  if (
    currentUser &&
    currentUser.id === userId.toString()
  ) {
    setCurrentUser({
      ...currentUser,
      status: frontendStatus,
    });
  }
};

const handleServerMembersChanged = (
  serverId: number
) => {
  window.dispatchEvent(
    new CustomEvent(
      "server-members:changed",
      {
        detail: {
          serverId,
        },
      }
    )
  );
};

const handleServerMemberRemoved = (
  serverId: number,
  message: string
) => {
  window.dispatchEvent(
    new CustomEvent(
      "server-membership:removed",
      {
        detail: {
          serverId,
          message,
        },
      }
    )
  );

  window.dispatchEvent(
    new Event("servers:refresh")
  );
};


const handleServerChannelsChanged = (
  serverId: number
) => {
  window.dispatchEvent(
    new CustomEvent(
      "server-channels:changed",
      {
        detail: {
          serverId,
        },
      }
    )
  );
};

const handleChannelAccessRevoked = (
  serverId: number,
  channelId: number,
  message: string
) => {
  window.dispatchEvent(
    new CustomEvent(
      "channel-access:revoked",
      {
        detail: {
          serverId,
          channelId,
          message,
        },
      }
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
    newConnection.on(
  "UserPresenceChanged",
  handleUserPresenceChanged
);

newConnection.on(
  "ServerMembersChanged",
  handleServerMembersChanged
);

newConnection.on(
  "ServerMemberRemoved",
  handleServerMemberRemoved
);

newConnection.on(
  "ServerChannelsChanged",
  handleServerChannelsChanged
);

newConnection.on(
  "ChannelAccessRevoked",
  handleChannelAccessRevoked
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

      newConnection.off(
  "UserPresenceChanged",
  handleUserPresenceChanged
);

newConnection.off(
  "ServerMembersChanged",
  handleServerMembersChanged
);

newConnection.off(
  "ServerMemberRemoved",
  handleServerMemberRemoved
);

newConnection.off(
  "ServerChannelsChanged",
  handleServerChannelsChanged
);

newConnection.off(
  "ChannelAccessRevoked",
  handleChannelAccessRevoked
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
  }, [hasAccessToken]);

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