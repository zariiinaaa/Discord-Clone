"use client";

import {
  type FormEvent,
  useCallback,
  useEffect,
  useRef,
  useState,
} from "react";

import {
  HubConnectionState,
} from "@microsoft/signalr";

import {
  createConversationMessage,
  deleteConversationMessage,
  getConversationById,
  getConversationMessages,
  markConversationAsRead,
  updateConversationMessage,
  updateConversationMuteStatus,
} from "@/lib/conversationApi";

import type {
  ConversationMessageResponse,
  ConversationResponse,
} from "@/lib/conversationApi";


import {
  useChatHub,
} from "@/components/chat/chat-hub-provider";

import {
  useVoice,
} from "@/components/voice/voice-provider";

import { useAuthStore } from "@/state/auth";
import { useCurrentUserStore } from "@/state/user";

interface ConversationDMProps {
  conversationId: number;
}

type RealtimeStatus =
  | "disconnected"
  | "connecting"
  | "connected"
  | "reconnecting";

export default function ConversationDM({
  conversationId,
}: ConversationDMProps) {
  const accessToken = useAuthStore(
    state => state.accessToken
  );

  const currentUser = useCurrentUserStore(
    state => state.currentUser
  );

const {
  connection: sharedConnection,
  status: chatHubStatus,
} = useChatHub();
const {
  activeConversationId,
  joinConversationVoice,
} = useVoice();


  const [conversation, setConversation] =
    useState<ConversationResponse | null>(null);

  const [messages, setMessages] =
    useState<ConversationMessageResponse[]>([]);

  const [messageText, setMessageText] =
    useState("");

  const [replyToMessage, setReplyToMessage] =
    useState<ConversationMessageResponse | null>(
      null
    );

  const [
    editingMessageId,
    setEditingMessageId,
  ] = useState<number | null>(null);

  const [editingContent, setEditingContent] =
    useState("");

  const [typingUserIds, setTypingUserIds] =
    useState<number[]>([]);

  const [isLoading, setIsLoading] =
    useState(true);

  const [isSending, setIsSending] =
    useState(false);

  const [isUpdatingMute, setIsUpdatingMute] =
    useState(false);

const [isJoiningVoice, setIsJoiningVoice] =
  useState(false);

const [voiceError, setVoiceError] =
  useState<string | null>(null);

  const [isSavingEdit, setIsSavingEdit] =
    useState(false);

  const [
    deletingMessageId,
    setDeletingMessageId,
  ] = useState<number | null>(null);

  const [error, setError] =
    useState<string | null>(null);

  const [sendError, setSendError] =
    useState<string | null>(null);

  const [realtimeStatus, setRealtimeStatus] =
    useState<RealtimeStatus>("disconnected");

  const [
    hasUnreadMessages,
    setHasUnreadMessages,
  ] = useState(false);



  const typingTimeoutRef =
    useRef<ReturnType<
      typeof setTimeout
    > | null>(null);

  const remoteTypingTimeoutsRef =
    useRef<
      Map<
        number,
        ReturnType<typeof setTimeout>
      >
    >(new Map());

  const typingSentRef =
    useRef(false);

  const lastTypingBroadcastRef =
    useRef(0);

  const messagesContainerRef =
    useRef<HTMLElement | null>(null);

  const messagesEndRef =
    useRef<HTMLDivElement | null>(null);

  const shouldAutoScrollRef =
    useRef(true);

  const previousMessageCountRef =
    useRef(0);

  const latestMessageIdRef =
    useRef<number | null>(null);

  const lastMarkedMessageIdRef =
    useRef<number | null>(null);

  const markingMessageIdRef =
    useRef<number | null>(null);

  const markCurrentConversationAsRead =
    useCallback(
      async (
        messageId?: number | null
      ) => {
        if (!accessToken) {
          return;
        }

        const latestMessageId =
          messageId ??
          latestMessageIdRef.current;

        if (
          latestMessageId === null ||
          latestMessageId ===
            lastMarkedMessageIdRef.current ||
          latestMessageId ===
            markingMessageIdRef.current
        ) {
          return;
        }

        markingMessageIdRef.current =
          latestMessageId;

        try {
          await markConversationAsRead(
            conversationId,
            accessToken
          );

          lastMarkedMessageIdRef.current =
            latestMessageId;
        } catch (markReadError) {
          console.error(
            "Conversation oxunmuş kimi qeyd edilə bilmədi:",
            markReadError
          );
        } finally {
          if (
            markingMessageIdRef.current ===
            latestMessageId
          ) {
            markingMessageIdRef.current =
              null;
          }
        }
      },
      [
        conversationId,
        accessToken,
      ]
    );

const sendTypingStatus =
  useCallback(
    async (isTyping: boolean) => {
      if (
        !sharedConnection ||
        sharedConnection.state !==
          HubConnectionState.Connected
      ) {
        return;
      }

      try {
        await sharedConnection.invoke(
          "SetConversationTyping",
          conversationId,
          isTyping
        );
      } catch (typingError) {
        console.error(
          "Typing status could not be sent:",
          typingError
        );
      }
    },
    [
      sharedConnection,
      conversationId,
    ]
  );

  const stopLocalTyping =
    useCallback(() => {
      if (typingTimeoutRef.current) {
        clearTimeout(
          typingTimeoutRef.current
        );

        typingTimeoutRef.current =
          null;
      }

      if (typingSentRef.current) {
        typingSentRef.current = false;

        lastTypingBroadcastRef.current =
          0;

        void sendTypingStatus(false);
      }
    }, [sendTypingStatus]);

  const handleMessageTextChange = (
    value: string
  ) => {
    setMessageText(value);

    if (typingTimeoutRef.current) {
      clearTimeout(
        typingTimeoutRef.current
      );

      typingTimeoutRef.current = null;
    }

    if (!value.trim()) {
      stopLocalTyping();
      return;
    }

    const currentTime = Date.now();

    const shouldBroadcastTyping =
      !typingSentRef.current ||
      currentTime -
        lastTypingBroadcastRef.current >=
        3000;

    if (shouldBroadcastTyping) {
      typingSentRef.current = true;

      lastTypingBroadcastRef.current =
        currentTime;

      void sendTypingStatus(true);
    }

    typingTimeoutRef.current =
      setTimeout(() => {
        typingSentRef.current = false;

        lastTypingBroadcastRef.current =
          0;

        typingTimeoutRef.current = null;

        void sendTypingStatus(false);
      }, 1500);
  };

  useEffect(() => {
    if (
      !Number.isInteger(conversationId) ||
      conversationId <= 0
    ) {
      setError(
        "Conversation ID düzgün deyil."
      );

      setIsLoading(false);
      return;
    }

    if (!accessToken) {
      setError(
        "Söhbəti görmək üçün hesabınıza daxil olun."
      );

      setIsLoading(false);
      return;
    }

    let isCancelled = false;

    const loadConversation = async () => {
      try {
        setIsLoading(true);
        setError(null);

        latestMessageIdRef.current =
          null;

        lastMarkedMessageIdRef.current =
          null;

        markingMessageIdRef.current =
          null;

        previousMessageCountRef.current =
          0;

        shouldAutoScrollRef.current =
          true;

        setHasUnreadMessages(false);

        const [
          conversationResponse,
          messagesResponse,
        ] = await Promise.all([
          getConversationById(
            conversationId,
            accessToken
          ),

          getConversationMessages(
            conversationId,
            accessToken
          ),
        ]);

        if (!isCancelled) {
          const latestMessage =
            messagesResponse[
              messagesResponse.length - 1
            ];

          latestMessageIdRef.current =
            latestMessage?.id ?? null;

          setConversation(
            conversationResponse
          );

          setMessages(messagesResponse);

          if (
            document.visibilityState ===
            "visible"
          ) {
            void markCurrentConversationAsRead(
              latestMessage?.id ?? null
            );
          }
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : "Söhbət yüklənə bilmədi."
          );
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    };

    void loadConversation();

    return () => {
      isCancelled = true;
    };
  }, [
    conversationId,
    accessToken,
    markCurrentConversationAsRead,

  ]);

useEffect(() => {
  if (
    !sharedConnection ||
    !accessToken ||
    !Number.isInteger(conversationId) ||
    conversationId <= 0
  ) {
    setRealtimeStatus("disconnected");
    return;
  }

  if (chatHubStatus !== "connected") {
    setRealtimeStatus(chatHubStatus);
    setTypingUserIds([]);
    return;
  }

  let isDisposed = false;

  const connection =
    sharedConnection;

    const removeTypingUser = (
      userId: number
    ) => {
      setTypingUserIds(
        currentIds =>
          currentIds.filter(
            currentId =>
              currentId !== userId
          )
      );

      const existingTimeout =
        remoteTypingTimeoutsRef.current.get(
          userId
        );

      if (existingTimeout) {
        clearTimeout(existingTimeout);

        remoteTypingTimeoutsRef.current.delete(
          userId
        );
      }
    };

    const handleMessageCreated = (
      eventConversationId: number,
      createdMessage:
        ConversationMessageResponse
    ) => {
      if (
        eventConversationId !==
        conversationId
      ) {
        return;
      }

      removeTypingUser(
        createdMessage.authorId
      );

      latestMessageIdRef.current =
        createdMessage.id;

      setMessages(currentMessages => {
        const alreadyExists =
          currentMessages.some(
            message =>
              message.id ===
              createdMessage.id
          );

        if (alreadyExists) {
          return currentMessages;
        }

        return [
          ...currentMessages,
          createdMessage,
        ];
      });

      const isIncomingMessage =
        createdMessage.authorId.toString() !==
        currentUser?.id;

      if (
        isIncomingMessage &&
        document.visibilityState ===
          "visible" &&
        shouldAutoScrollRef.current
      ) {
        void markCurrentConversationAsRead(
          createdMessage.id
        );
      }
    };

    const handleMessageUpdated = (
      eventConversationId: number,
      updatedMessage:
        ConversationMessageResponse
    ) => {
      if (
        eventConversationId !==
        conversationId
      ) {
        return;
      }

      setMessages(currentMessages =>
        currentMessages.map(message =>
          message.id ===
          updatedMessage.id
            ? updatedMessage
            : message
        )
      );

      setReplyToMessage(
        currentReply =>
          currentReply?.id ===
          updatedMessage.id
            ? updatedMessage
            : currentReply
      );
    };

    const handleMessageDeleted = (
      eventConversationId: number,
      messageId: number
    ) => {
      if (
        eventConversationId !==
        conversationId
      ) {
        return;
      }

      setMessages(currentMessages => {
        const remainingMessages =
          currentMessages.filter(
            message =>
              message.id !== messageId
          );

        latestMessageIdRef.current =
          remainingMessages[
            remainingMessages.length - 1
          ]?.id ?? null;

        return remainingMessages;
      });

      setReplyToMessage(
        currentReply =>
          currentReply?.id === messageId
            ? null
            : currentReply
      );

      setEditingMessageId(
        currentId =>
          currentId === messageId
            ? null
            : currentId
      );
    };

    const handleTypingChanged = (
      eventConversationId: number,
      userId: number,
      isTyping: boolean
    ) => {
      if (
        eventConversationId !==
        conversationId
      ) {
        return;
      }

      const existingTimeout =
        remoteTypingTimeoutsRef.current.get(
          userId
        );

      if (existingTimeout) {
        clearTimeout(existingTimeout);

        remoteTypingTimeoutsRef.current.delete(
          userId
        );
      }

      if (!isTyping) {
        setTypingUserIds(
          currentIds =>
            currentIds.filter(
              currentId =>
                currentId !== userId
            )
        );

        return;
      }

      setTypingUserIds(
        currentIds =>
          currentIds.includes(userId)
            ? currentIds
            : [
                ...currentIds,
                userId,
              ]
      );

      const timeout =
        setTimeout(() => {
          setTypingUserIds(
            currentIds =>
              currentIds.filter(
                currentId =>
                  currentId !== userId
              )
          );

          remoteTypingTimeoutsRef.current.delete(
            userId
          );
        }, 5000);

      remoteTypingTimeoutsRef.current.set(
        userId,
        timeout
      );
    };

    const handleDirectMessageRequestCreated =
      () => {
        window.dispatchEvent(
          new Event(
            "message-requests:refresh"
          )
        );
      };

    connection.on(
      "ConversationMessageCreated",
      handleMessageCreated
    );

    connection.on(
      "ConversationMessageUpdated",
      handleMessageUpdated
    );

    connection.on(
      "ConversationMessageDeleted",
      handleMessageDeleted
    );

    connection.on(
      "ConversationTypingChanged",
      handleTypingChanged
    );

    connection.on(
      "DirectMessageRequestCreated",
      handleDirectMessageRequestCreated
    );

   const joinConversation =
  async () => {
    try {
      await connection.invoke(
        "JoinConversation",
        conversationId
      );

      if (!isDisposed) {
        setRealtimeStatus(
          "connected"
        );
      }
    } catch (connectionError) {
      if (!isDisposed) {
        console.error(
          "DM conversation could not be joined:",
          connectionError
        );

        setRealtimeStatus(
          "disconnected"
        );
      }
    }
  };

void joinConversation();

    return () => {
      isDisposed = true;

      connection.off(
        "ConversationMessageCreated",
        handleMessageCreated
      );

      connection.off(
        "ConversationMessageUpdated",
        handleMessageUpdated
      );

      connection.off(
        "ConversationMessageDeleted",
        handleMessageDeleted
      );

      connection.off(
        "ConversationTypingChanged",
        handleTypingChanged
      );

      connection.off(
        "DirectMessageRequestCreated",
        handleDirectMessageRequestCreated
      );

      if (
        typingTimeoutRef.current
      ) {
        clearTimeout(
          typingTimeoutRef.current
        );

        typingTimeoutRef.current =
          null;
      }

      remoteTypingTimeoutsRef.current.forEach(
        timeout =>
          clearTimeout(timeout)
      );

      remoteTypingTimeoutsRef.current.clear();

      typingSentRef.current = false;

      lastTypingBroadcastRef.current =
        0;

      

      setTypingUserIds([]);

   if (
  connection.state ===
  HubConnectionState.Connected
) {
  void connection
    .invoke(
      "SetConversationTyping",
      conversationId,
      false
    )
    .catch(typingError => {
      console.error(
        "Typing status could not be stopped:",
        typingError
      );
    });
}

/*
 * Connection qlobaldır.
 * Burada LeaveConversation və
 * connection.stop çağırılmır.
 */
    };
  }, [
    conversationId,
    accessToken,
    currentUser?.id,
    markCurrentConversationAsRead,
    sharedConnection,
    chatHubStatus,
  ]);

  const scrollToBottom =
    useCallback(
      (
        behavior: ScrollBehavior =
          "smooth"
      ) => {
        window.requestAnimationFrame(
          () => {
            messagesEndRef.current?.scrollIntoView(
              {
                behavior,
                block: "end",
              }
            );

            void markCurrentConversationAsRead();
          }
        );

        shouldAutoScrollRef.current =
          true;

        setHasUnreadMessages(false);
      },
      [markCurrentConversationAsRead]
    );

  const handleMessagesScroll = () => {
    const container =
      messagesContainerRef.current;

    if (!container) {
      return;
    }

    const distanceFromBottom =
      container.scrollHeight -
      container.scrollTop -
      container.clientHeight;

    const isNearBottom =
      distanceFromBottom <= 120;

    shouldAutoScrollRef.current =
      isNearBottom;

    if (isNearBottom) {
      setHasUnreadMessages(false);

      void markCurrentConversationAsRead();
    }
  };

  useEffect(() => {
    const markWhenVisible = () => {
      if (
        document.visibilityState ===
          "visible" &&
        shouldAutoScrollRef.current
      ) {
        void markCurrentConversationAsRead();
      }
    };

    window.addEventListener(
      "focus",
      markWhenVisible
    );

    document.addEventListener(
      "visibilitychange",
      markWhenVisible
    );

    return () => {
      window.removeEventListener(
        "focus",
        markWhenVisible
      );

      document.removeEventListener(
        "visibilitychange",
        markWhenVisible
      );
    };
  }, [
    markCurrentConversationAsRead,
  ]);

useEffect(() => {
  const previousMessageCount =
    previousMessageCountRef.current;

  const hasNewMessage =
    messages.length >
    previousMessageCount;

  const hasDeletedMessage =
    messages.length <
    previousMessageCount;

  if (hasNewMessage) {
    if (
      previousMessageCount === 0 ||
      shouldAutoScrollRef.current
    ) {
      scrollToBottom(
        previousMessageCount === 0
          ? "auto"
          : "smooth"
      );
    } else {
      setHasUnreadMessages(true);
    }
  }

  if (
    hasDeletedMessage &&
    shouldAutoScrollRef.current
  ) {
    scrollToBottom("auto");
  }

  previousMessageCountRef.current =
    messages.length;
}, [
  messages,
  scrollToBottom,
]);

  const handleSendMessage = async (
    event: FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault();

    const content = messageText.trim();

    if (
      !content ||
      !accessToken ||
      isSending
    ) {
      return;
    }

    stopLocalTyping();

    try {
      setIsSending(true);
      setSendError(null);

      const createdMessage =
        await createConversationMessage(
          conversationId,
          {
            content,
            replyToMessageId:
              replyToMessage?.id ?? null,
          },
          accessToken
        );

      latestMessageIdRef.current =
        createdMessage.id;

      setMessages(currentMessages => {
        const alreadyExists =
          currentMessages.some(
            message =>
              message.id ===
              createdMessage.id
          );

        if (alreadyExists) {
          return currentMessages;
        }

        return [
          ...currentMessages,
          createdMessage,
        ];
      });

      setMessageText("");
      setReplyToMessage(null);

      scrollToBottom("smooth");

      void markCurrentConversationAsRead(
        createdMessage.id
      );
    } catch (sendMessageError) {
      setSendError(
        sendMessageError instanceof Error
          ? sendMessageError.message
          : "Mesaj göndərilə bilmədi."
      );
    } finally {
      setIsSending(false);
    }
  };

  const beginReply = (
    message: ConversationMessageResponse
  ) => {
    setReplyToMessage(message);

    setEditingMessageId(null);
    setEditingContent("");
    setSendError(null);
  };

  const beginEditing = (
    message: ConversationMessageResponse
  ) => {
    setEditingMessageId(message.id);

    setEditingContent(
      message.content
    );

    setReplyToMessage(null);
    setSendError(null);
  };

  const cancelEditing = () => {
    setEditingMessageId(null);
    setEditingContent("");
  };

  const saveEditedMessage = async (
    messageId: number
  ) => {
    const content =
      editingContent.trim();

    if (
      !content ||
      !accessToken ||
      isSavingEdit
    ) {
      return;
    }

    try {
      setIsSavingEdit(true);
      setSendError(null);

      const updatedMessage =
        await updateConversationMessage(
          conversationId,
          messageId,
          { content },
          accessToken
        );

      setMessages(currentMessages =>
        currentMessages.map(message =>
          message.id ===
          updatedMessage.id
            ? updatedMessage
            : message
        )
      );

      setEditingMessageId(null);
      setEditingContent("");
    } catch (updateError) {
      setSendError(
        updateError instanceof Error
          ? updateError.message
          : "Mesaj redaktə edilə bilmədi."
      );
    } finally {
      setIsSavingEdit(false);
    }
  };

  const removeMessage = async (
    messageId: number
  ) => {
    if (
      !accessToken ||
      deletingMessageId !== null
    ) {
      return;
    }

    const shouldDelete =
      window.confirm(
        "Bu mesajı silmək istədiyinizə əminsiniz?"
      );

    if (!shouldDelete) {
      return;
    }

    try {
      setDeletingMessageId(messageId);
      setSendError(null);

      await deleteConversationMessage(
        conversationId,
        messageId,
        accessToken
      );

      setMessages(currentMessages => {
        const remainingMessages =
          currentMessages.filter(
            message =>
              message.id !== messageId
          );

        latestMessageIdRef.current =
          remainingMessages[
            remainingMessages.length - 1
          ]?.id ?? null;

        return remainingMessages;
      });

      setReplyToMessage(
        currentReply =>
          currentReply?.id === messageId
            ? null
            : currentReply
      );

      if (
        editingMessageId === messageId
      ) {
        cancelEditing();
      }
    } catch (deleteError) {
      setSendError(
        deleteError instanceof Error
          ? deleteError.message
          : "Mesaj silinə bilmədi."
      );
    } finally {
      setDeletingMessageId(null);
    }
  };

  const scrollToMessage = (
    messageId: number
  ) => {
    document
      .getElementById(
        `conversation-message-${messageId}`
      )
      ?.scrollIntoView({
        behavior: "smooth",
        block: "center",
      });
  };

  const handleToggleMute =
    async () => {
      if (
        !conversation ||
        !accessToken ||
        isUpdatingMute
      ) {
        return;
      }

      const currentMember =
        conversation.members.find(
          member =>
            member.userId.toString() ===
            currentUser?.id
        );

      if (!currentMember) {
        setSendError(
          "Cari istifadəçi söhbətin üzvü kimi tapılmadı."
        );

        return;
      }

      const nextMutedStatus =
        !currentMember.isMuted;

      try {
        setIsUpdatingMute(true);
        setSendError(null);

        await updateConversationMuteStatus(
          conversationId,
          {
            isMuted:
              nextMutedStatus,
          },
          accessToken
        );

        setConversation(
          currentConversation => {
            if (!currentConversation) {
              return currentConversation;
            }

            return {
              ...currentConversation,

              members:
                currentConversation.members.map(
                  member =>
                    member.userId ===
                    currentMember.userId
                      ? {
                          ...member,
                          isMuted:
                            nextMutedStatus,
                        }
                      : member
                ),
            };
          }
        );
      } catch (muteError) {
        setSendError(
          muteError instanceof Error
            ? muteError.message
            : "Bildiriş statusu dəyişdirilə bilmədi."
        );
      } finally {
        setIsUpdatingMute(false);
      }
    };

const handleJoinVoice = async () => {
  if (
    isJoiningVoice ||
    activeConversationId === conversationId
  ) {
    return;
  }

  try {
    setIsJoiningVoice(true);
    setVoiceError(null);

    await joinConversationVoice(
      conversationId
    );
  } catch (joinError) {
    setVoiceError(
      joinError instanceof Error
        ? joinError.message
        : "DM zənginə qoşulmaq alınmadı."
    );
  } finally {
    setIsJoiningVoice(false);
  }
};

  if (isLoading) {
    return (
      <main className="ml-[330px] flex min-h-screen items-center justify-center bg-background text-gray-300">
        Söhbət yüklənir...
      </main>
    );
  }

  if (
    error ||
    !conversation
  ) {
    return (
      <main className="ml-[330px] flex min-h-screen items-center justify-center bg-background">
        <div className="rounded-lg bg-red-950/40 px-6 py-4 text-red-300">
          {error ??
            "Söhbət tapılmadı."}
        </div>
      </main>
    );
  }

  const currentConversationMember =
    conversation.members.find(
      member =>
        member.userId.toString() ===
        currentUser?.id
    );

  const isConversationMuted =
    currentConversationMember?.isMuted ??
    false;

  const otherMembers =
    conversation.members.filter(
      member =>
        member.userId.toString() !==
        currentUser?.id
    );

  const directMessageName =
    otherMembers
      .map(
        member =>
          member.displayName
      )
      .join(", ");

  const conversationTitle =
    conversation.name ||
    directMessageName ||
    "Şəxsi söhbət";

  const realtimeText = {
    disconnected:
      "Real-time bağlı deyil",

    connecting:
      "Real-time qoşulur...",

    connected:
      "Real-time bağlıdır",

    reconnecting:
      "Real-time yenidən qoşulur...",
  }[realtimeStatus];

  const typingMembers =
    conversation.members.filter(
      member =>
        typingUserIds.includes(
          member.userId
        )
    );

  let typingText = "";

  if (
    typingMembers.length === 1
  ) {
    typingText =
      `${typingMembers[0].displayName} yazır...`;
  } else if (
    typingMembers.length === 2
  ) {
    typingText =
      `${typingMembers[0].displayName} və ` +
      `${typingMembers[1].displayName} yazır...`;
  } else if (
    typingMembers.length > 2
  ) {
    typingText =
      "Bir neçə nəfər yazır...";
  } else if (
    typingUserIds.length > 0
  ) {
    typingText = "Kimsə yazır...";
  }

  return (
    <main className="ml-[330px] flex h-screen min-w-0 flex-col bg-background text-white">
      <header className="flex h-[72px] flex-none items-center border-b border-black/20 px-6 shadow">
        <div className="min-w-0">
          <h1 className="truncate font-semibold">
            {conversationTitle}
          </h1>

          <p className="text-xs text-gray-400">
            {conversation.type === 0
              ? "Direct Message"
              : `${conversation.members.length} üzv`}
          </p>
        </div>

        <div className="ml-auto flex items-center gap-4">
          <button
  type="button"
  onClick={() => void handleJoinVoice()}
  disabled={
    isJoiningVoice ||
    activeConversationId === conversationId
  }
  className="rounded-md bg-green-500/15 px-3 py-2 text-xs font-semibold text-green-300 transition-colors hover:bg-green-500/25 disabled:cursor-not-allowed disabled:opacity-50"
>
  {isJoiningVoice
    ? "Qoşulur..."
    : activeConversationId === conversationId
      ? "📞 Zəngdəsiniz"
      : "📞 Zəng et"}
</button>
          <button
            type="button"
            onClick={() =>
              void handleToggleMute()
            }
            disabled={
              isUpdatingMute
            }
            aria-pressed={
              isConversationMuted
            }
            className={`rounded-md px-3 py-2 text-xs font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${
              isConversationMuted
                ? "bg-yellow-500/15 text-yellow-300 hover:bg-yellow-500/25"
                : "bg-white/5 text-gray-300 hover:bg-white/10 hover:text-white"
            }`}
          >
            {isUpdatingMute
              ? "Dəyişdirilir..."
              : isConversationMuted
                ? "🔕 Bildirişləri aktiv et"
                : "🔔 Bildirişləri susdur"}
          </button>

          <div className="flex items-center gap-2 text-xs text-gray-400">
            <span
              className={`h-2 w-2 rounded-full ${
                realtimeStatus ===
                "connected"
                  ? "bg-green-500"
                  : realtimeStatus ===
                      "disconnected"
                    ? "bg-red-500"
                    : "bg-yellow-500"
              }`}
            />

            {realtimeText}
          </div>
        </div>
      </header>

      <div className="relative min-h-0 flex-1">
        <section
          ref={
            messagesContainerRef
          }
          onScroll={
            handleMessagesScroll
          }
          className="h-full overflow-y-auto px-6 py-5"
        >
          {messages.length === 0 ? (
            <div className="flex h-full items-center justify-center text-gray-500">
              Bu söhbətdə hələ mesaj
              yoxdur.
            </div>
          ) : (
            <div className="space-y-1">
              {messages.map(
                message => {
                  const authorInitial =
                    message.authorDisplayName
                      .charAt(0)
                      .toUpperCase();

                  const isOwnMessage =
                    message.authorId.toString() ===
                    currentUser?.id;

                  const isEditing =
                    editingMessageId ===
                    message.id;

                  const repliedMessage =
                    message.replyToMessageId
                      ? messages.find(
                          item =>
                            item.id ===
                            message.replyToMessageId
                        )
                      : null;

                  return (
                    <article
                      id={`conversation-message-${message.id}`}
                      key={
                        message.id
                      }
                      className="group relative flex gap-3 rounded-md px-2 py-2 hover:bg-white/[0.04]"
                    >
                      <div className="flex h-11 w-11 flex-none items-center justify-center rounded-full bg-primary font-semibold">
                        {
                          authorInitial
                        }
                      </div>

                      <div className="min-w-0 flex-1">
                        {message.replyToMessageId && (
                          <button
                            type="button"
                            onClick={() =>
                              scrollToMessage(
                                message.replyToMessageId!
                              )
                            }
                            className="mb-1 block max-w-full border-l-2 border-gray-500 pl-2 text-left text-xs text-gray-400 hover:text-gray-200"
                          >
                            <span className="font-semibold">
                              {repliedMessage?.authorDisplayName ??
                                "Əvvəlki mesaj"}
                            </span>

                            <span className="ml-2 inline-block max-w-[500px] truncate align-bottom">
                              {repliedMessage?.content ??
                                `Mesaj #${message.replyToMessageId}`}
                            </span>
                          </button>
                        )}

                        <div className="flex flex-wrap items-center gap-2">
                          <span className="font-semibold">
                            {
                              message.authorDisplayName
                            }
                          </span>

                          <time className="text-xs text-gray-500">
                            {new Date(
                              message.createdAt
                            ).toLocaleString(
                              "az-AZ"
                            )}
                          </time>

                          {message.editedAt && (
                            <span className="text-xs text-gray-500">
                              (redaktə edilib)
                            </span>
                          )}
                        </div>

                        {isEditing ? (
                          <div className="mt-2">
                            <input
                              type="text"
                              value={
                                editingContent
                              }
                              onChange={event =>
                                setEditingContent(
                                  event.target.value
                                )
                              }
                              onKeyDown={event => {
                                if (
                                  event.key ===
                                  "Enter"
                                ) {
                                  event.preventDefault();

                                  void saveEditedMessage(
                                    message.id
                                  );
                                }

                                if (
                                  event.key ===
                                  "Escape"
                                ) {
                                  cancelEditing();
                                }
                              }}
                              disabled={
                                isSavingEdit
                              }
                              maxLength={
                                2000
                              }
                              autoFocus
                              className="w-full rounded-md bg-white/10 px-3 py-2 text-white outline-none focus:ring-1 focus:ring-primary"
                            />

                            <div className="mt-2 flex items-center gap-2 text-xs">
                              <button
                                type="button"
                                onClick={() =>
                                  void saveEditedMessage(
                                    message.id
                                  )
                                }
                                disabled={
                                  isSavingEdit ||
                                  !editingContent.trim()
                                }
                                className="rounded bg-primary px-3 py-1.5 font-semibold disabled:opacity-40"
                              >
                                {isSavingEdit
                                  ? "Saxlanılır..."
                                  : "Yadda saxla"}
                              </button>

                              <button
                                type="button"
                                onClick={
                                  cancelEditing
                                }
                                disabled={
                                  isSavingEdit
                                }
                                className="rounded px-3 py-1.5 text-gray-300 hover:bg-white/10"
                              >
                                Ləğv et
                              </button>
                            </div>
                          </div>
                        ) : (
                          <p className="mt-1 whitespace-pre-wrap break-words text-gray-100">
                            {
                              message.content
                            }
                          </p>
                        )}
                      </div>

                      {!isEditing && (
                        <div className="absolute right-3 top-2 flex items-center gap-1 rounded-md border border-black/30 bg-semibackground p-1 opacity-0 shadow transition-opacity group-hover:opacity-100 group-focus-within:opacity-100">
                          <button
                            type="button"
                            onClick={() =>
                              beginReply(
                                message
                              )
                            }
                            className="rounded px-2 py-1 text-xs text-gray-300 hover:bg-white/10 hover:text-white"
                          >
                            Cavab ver
                          </button>

                          {isOwnMessage && (
                            <>
                              <button
                                type="button"
                                onClick={() =>
                                  beginEditing(
                                    message
                                  )
                                }
                                className="rounded px-2 py-1 text-xs text-gray-300 hover:bg-white/10 hover:text-white"
                              >
                                Redaktə et
                              </button>

                              <button
                                type="button"
                                onClick={() =>
                                  void removeMessage(
                                    message.id
                                  )
                                }
                                disabled={
                                  deletingMessageId ===
                                  message.id
                                }
                                className="rounded px-2 py-1 text-xs text-red-400 hover:bg-red-500/10 disabled:opacity-40"
                              >
                                {deletingMessageId ===
                                message.id
                                  ? "Silinir..."
                                  : "Sil"}
                              </button>
                            </>
                          )}
                        </div>
                      )}
                    </article>
                  );
                }
              )}
            </div>
          )}

          <div
            ref={messagesEndRef}
            className="h-px"
          />
        </section>

        {hasUnreadMessages && (
          <button
            type="button"
            onClick={() =>
              scrollToBottom("smooth")
            }
            className="absolute bottom-4 left-1/2 -translate-x-1/2 rounded-full bg-primary px-4 py-2 text-sm font-semibold text-white shadow-lg hover:brightness-110"
          >
            Yeni mesajlar ↓
          </button>
        )}
      </div>

      <footer className="flex-none px-6 pb-6">
        <div className="h-6 px-1 text-xs font-semibold text-gray-300">
          {typingText}
        </div>
{voiceError && (
  <p className="mb-2 text-sm text-red-400">
    {voiceError}
  </p>
)}
        {sendError && (
          <p className="mb-2 text-sm text-red-400">
            {sendError}
          </p>
        )}

        {replyToMessage && (
          <div className="flex items-center gap-3 rounded-t-lg border-b border-black/20 bg-white/10 px-5 py-2 text-sm">
            <div className="min-w-0 flex-1">
              <p className="text-xs text-gray-400">
                Cavab verilir:
                <span className="ml-1 font-semibold text-gray-200">
                  {
                    replyToMessage.authorDisplayName
                  }
                </span>
              </p>

              <p className="truncate text-gray-300">
                {
                  replyToMessage.content
                }
              </p>
            </div>

            <button
              type="button"
              onClick={() =>
                setReplyToMessage(null)
              }
              aria-label="Cavabı ləğv et"
              className="rounded px-2 py-1 text-gray-400 hover:bg-white/10 hover:text-white"
            >
              ✕
            </button>
          </div>
        )}

        <form
          onSubmit={
            handleSendMessage
          }
          className={`flex items-center bg-white/10 ${
            replyToMessage
              ? "rounded-b-lg"
              : "rounded-lg"
          }`}
        >
          <input
            type="text"
            value={messageText}
            onChange={event =>
              handleMessageTextChange(
                event.target.value
              )
            }
            placeholder={`@${conversationTitle} üçün mesaj yaz`}
            disabled={isSending}
            maxLength={2000}
            autoComplete="off"
            className="min-w-0 flex-1 bg-transparent px-5 py-4 text-white outline-none placeholder:text-gray-500 disabled:opacity-60"
          />

          <button
            type="submit"
            disabled={
              isSending ||
              !messageText.trim()
            }
            className="mr-2 rounded-md bg-primary px-4 py-2 text-sm font-semibold hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-40"
          >
            {isSending
              ? "Göndərilir..."
              : "Göndər"}
          </button>
        </form>
      </footer>
    </main>
  );
}