"use client";

import {
  useEffect,
  useRef,
  useState,
} from "react";

import Link from "next/link";
import { useParams } from "next/navigation";



import { List } from "@/components/ui/list";
import DMChannelListHeader from "./dm-channel-list-header";

import {
  getMyConversations,
} from "@/lib/conversationApi";

import type {
  ConversationMemberResponse,
  ConversationMessageResponse,
  ConversationResponse,
} from "@/lib/conversationApi";

import {
  useChatHub,
} from "@/components/chat/chat-hub-provider";

import type {
  ListedDMChannel,
} from "@/lib/entities/channel";

import { useAuthStore } from "@/state/auth";
import { useCurrentUserStore } from "@/state/user";

interface DMChannelListProps {
  channelsData: ListedDMChannel[];
}

export default function DMChannelList(
  _props: DMChannelListProps
) {
  const params = useParams<{
    id?: string;
  }>();

  const accessToken = useAuthStore(
    state => state.accessToken
  );

  const currentUser = useCurrentUserStore(
    state => state.currentUser
  );

  const {
  connection,
  status: chatHubStatus,
} = useChatHub();

  const [conversations, setConversations] =
    useState<ConversationResponse[]>([]);

  const [isLoading, setIsLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(null);

  const activeConversationIdRef =
    useRef<number | null>(null);

  /*
   * Conversation siyahısını backend-dən
   * ilkin dəfə yükləyir.
   */
  useEffect(() => {
    if (!accessToken) {
      setConversations([]);
      setIsLoading(false);
      return;
    }

    let isCancelled = false;

    const loadConversations = async () => {
      try {
        setIsLoading(true);
        setError(null);

        const response =
          await getMyConversations(
            accessToken
          );

        if (!isCancelled) {
          setConversations(response);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : "DM siyahısı yüklənə bilmədi."
          );
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    };

    void loadConversations();

    return () => {
      isCancelled = true;
    };
  }, [accessToken]);

    /*
   * Message Request qəbul edildikdə
   * conversation siyahısını refreshsiz yeniləyir.
   */
  useEffect(() => {
    if (!accessToken) {
      return;
    }

    let isDisposed = false;

    const handleConversationsRefresh =
      async () => {
        try {
          const response =
            await getMyConversations(
              accessToken
            );

          if (isDisposed) {
            return;
          }

          const activeConversationId =
            activeConversationIdRef.current;

          setConversations(
            response.map(conversation =>
              conversation.id ===
              activeConversationId
                ? {
                    ...conversation,
                    unreadCount: 0,
                  }
                : conversation
            )
          );

          setError(null);
        } catch (refreshError) {
          console.error(
            "DM list could not be refreshed:",
            refreshError
          );
        }
      };

    window.addEventListener(
      "conversations:refresh",
      handleConversationsRefresh
    );

    return () => {
      isDisposed = true;

      window.removeEventListener(
        "conversations:refresh",
        handleConversationsRefresh
      );
    };
  }, [accessToken]);
  /*
   * Hazırda açıq olan conversation ID-sini
   * yadda saxlayır və onun badge-ni gizlədir.
   */
  useEffect(() => {
    if (!params.id) {
      activeConversationIdRef.current =
        null;

      return;
    }

    const activeConversationId =
      Number(params.id);

    if (
      !Number.isInteger(
        activeConversationId
      ) ||
      activeConversationId <= 0
    ) {
      activeConversationIdRef.current =
        null;

      return;
    }

    activeConversationIdRef.current =
      activeConversationId;

    setConversations(
      currentConversations =>
        currentConversations.map(
          conversation =>
            conversation.id ===
            activeConversationId
              ? {
                  ...conversation,
                  unreadCount: 0,
                }
              : conversation
        )
    );
  }, [params.id]);

  /*
   * Conversation ID-lərini stabil şəkildə
   * dependency kimi istifadə etmək üçündür.
   */
  const conversationIds =
    conversations
      .map(
        conversation =>
          conversation.id
      )
      .sort(
        (first, second) =>
          first - second
      )
      .join(",");

/*
 * DM siyahısını qlobal SignalR
 * bağlantısına qoşur.
 *
 * Yeni mesaj gələndə unread badge
 * yenilənir və söhbət yuxarı daşınır.
 */
useEffect(() => {
  if (
    !connection ||
    chatHubStatus !== "connected" ||
    !accessToken ||
    !conversationIds
  ) {
    return;
  }

  const ids =
    conversationIds
      .split(",")
      .map(Number)
      .filter(
        conversationId =>
          Number.isInteger(
            conversationId
          ) &&
          conversationId > 0
      );

  if (ids.length === 0) {
    return;
  }

  let isDisposed = false;

  const joinAllConversations =
    async () => {
      for (
        const conversationId of ids
      ) {
        await connection.invoke(
          "JoinConversation",
          conversationId
        );
      }
    };

  const refreshConversations =
    async () => {
      try {
        const response =
          await getMyConversations(
            accessToken
          );

        if (isDisposed) {
          return;
        }

        const activeConversationId =
          activeConversationIdRef.current;

        setConversations(
          response.map(
            conversation =>
              conversation.id ===
              activeConversationId
                ? {
                    ...conversation,
                    unreadCount: 0,
                  }
                : conversation
          )
        );
      } catch (refreshError) {
        console.error(
          "DM list could not be refreshed:",
          refreshError
        );
      }
    };

  const handleMessageCreated = (
    eventConversationId: number,
    createdMessage:
      ConversationMessageResponse
  ) => {
    const activeConversationId =
      activeConversationIdRef.current;

    const isOwnMessage =
      createdMessage.authorId.toString() ===
      currentUser?.id;

    const shouldIncreaseUnread =
      !isOwnMessage &&
      activeConversationId !==
        eventConversationId;

    setConversations(
      currentConversations => {
        const targetConversation =
          currentConversations.find(
            conversation =>
              conversation.id ===
              eventConversationId
          );

        if (!targetConversation) {
          return currentConversations;
        }

        const updatedConversation = {
          ...targetConversation,

          unreadCount:
            shouldIncreaseUnread
              ? targetConversation.unreadCount +
                1
              : targetConversation.unreadCount,
        };

        return [
          updatedConversation,

          ...currentConversations.filter(
            conversation =>
              conversation.id !==
              eventConversationId
          ),
        ];
      }
    );
  };

  connection.on(
    "ConversationMessageCreated",
    handleMessageCreated
  );

  const initialiseRealtimeList =
    async () => {
      try {
        await joinAllConversations();

        if (!isDisposed) {
          await refreshConversations();
        }
      } catch (connectionError) {
        if (!isDisposed) {
          console.error(
            "DM list could not join conversations:",
            connectionError
          );
        }
      }
    };

  void initialiseRealtimeList();

  return () => {
    isDisposed = true;

    connection.off(
      "ConversationMessageCreated",
      handleMessageCreated
    );

    /*
     * Ortaq bağlantı olduğu üçün burada
     * connection.stop() çağırılmır.
     */
  };
}, [
  connection,
  chatHubStatus,
  accessToken,
  conversationIds,
  currentUser?.id,
]);

  const getOtherMembers = (
    conversation: ConversationResponse
  ): ConversationMemberResponse[] => {
    return conversation.members.filter(
      member =>
        member.userId.toString() !==
        currentUser?.id
    );
  };

  const getConversationTitle = (
    conversation: ConversationResponse
  ): string => {
    if (conversation.name) {
      return conversation.name;
    }

    const memberNames =
      getOtherMembers(conversation)
        .map(
          member =>
            member.displayName
        )
        .join(", ");

    return (
      memberNames ||
      "Şəxsi söhbət"
    );
  };

  const formatUnreadCount = (
    unreadCount: number
  ): string => {
    if (unreadCount > 99) {
      return "99+";
    }

    return unreadCount.toString();
  };

  return (
    <div className="pt-4">
      <DMChannelListHeader />

      <List className="mt-1">
        {isLoading && (
          <li className="px-3 py-3 text-sm text-gray-500">
            Söhbətlər yüklənir...
          </li>
        )}

        {!isLoading &&
          error && (
            <li className="px-3 py-3 text-sm text-red-400">
              {error}
            </li>
          )}

        {!isLoading &&
          !error &&
          conversations.length ===
            0 && (
            <li className="px-3 py-3 text-sm text-gray-500">
              Hələ söhbət yoxdur.
            </li>
          )}

        {!isLoading &&
          !error &&
          conversations.map(
            conversation => {
              const title =
                getConversationTitle(
                  conversation
                );

              const otherMembers =
                getOtherMembers(
                  conversation
                );

              const isActive =
                params.id ===
                conversation.id.toString();

              const subtitle =
                conversation.type === 0
                  ? otherMembers[0]
                      ?.username
                    ? `@${otherMembers[0].username}`
                    : "Direct Message"
                  : `${conversation.members.length} üzv`;

              const hasUnreadMessages =
                conversation.unreadCount >
                  0 &&
                !isActive;

              return (
                <li
                  key={
                    conversation.id
                  }
                  className="list-none"
                >
                  <Link
                    href={`/channels/${conversation.id}`}
                    className={`flex items-center gap-3 rounded-md px-3 py-2 ${
                      isActive
                        ? "bg-white/10 text-white"
                        : "text-gray-400 hover:bg-white/5 hover:text-gray-200"
                    }`}
                  >
                    <div className="relative flex h-9 w-9 flex-none items-center justify-center rounded-full bg-primary font-semibold text-white">
                      {title
                        .charAt(0)
                        .toUpperCase()}

                      {hasUnreadMessages && (
                        <span className="absolute -left-[17px] h-2 w-2 rounded-full bg-white" />
                      )}
                    </div>

                    <div className="min-w-0 flex-1">
                      <p
                        className={`truncate text-sm ${
                          hasUnreadMessages
                            ? "font-semibold text-white"
                            : "font-medium"
                        }`}
                      >
                        {title}
                      </p>

                      <p className="truncate text-xs text-gray-500">
                        {subtitle}
                      </p>
                    </div>

                    {hasUnreadMessages && (
                      <span
                        aria-label={`${conversation.unreadCount} oxunmamış mesaj`}
                        className="flex min-w-5 flex-none items-center justify-center rounded-full bg-red-500 px-1.5 py-0.5 text-[11px] font-bold leading-none text-white"
                      >
                        {formatUnreadCount(
                          conversation.unreadCount
                        )}
                      </span>
                    )}
                  </Link>
                </li>
              );
            }
          )}
      </List>
    </div>
  );
}