"use client";

import Link from "next/link";
import {
  useEffect,
  useRef,
  useState,
} from "react";
import type {
  ChangeEvent,
  FormEvent,
} from "react";

import { useParams } from "next/navigation";

import {
  getServerById,
  type ServerDetailsResponse,
} from "@/lib/serverApi";

import {
  createMessage,
  deleteMessage,
  getChannelMessages,
  updateMessage,
  type MessageResponse,
} from "@/lib/messageApi";

import {
  HubConnectionState,
} from "@microsoft/signalr";

import {
  useChatHub,
} from "@/components/chat/chat-hub-provider";
import { useAuthStore } from "@/state/auth";
import { useCurrentUserStore } from "@/state/user";
import { API_URL } from "@/lib/api";

const TEXT_CHANNEL_TYPE = 0;

function getAvatarUrl(
  avatarUrl: string | null
): string | null {
  if (!avatarUrl) {
    return null;
  }

  if (
    avatarUrl.startsWith("http://") ||
    avatarUrl.startsWith("https://")
  ) {
    return avatarUrl;
  }

  return `${API_URL}${
    avatarUrl.startsWith("/") ? "" : "/"
  }${avatarUrl}`;
}

export default function ChannelPage() {
  const params = useParams<{
    serverId: string;
    channelId: string;
  }>();

  const accessToken = useAuthStore(
    state => state.accessToken
  );

  const currentUser =
    useCurrentUserStore(
      state => state.currentUser
    );

  const {
  connection,
  status: chatHubStatus,
} = useChatHub();


 

  const messageInputRef =
    useRef<HTMLInputElement | null>(null);
    const fileInputRef =
  useRef<HTMLInputElement | null>(null);
const messagesContainerRef =
  useRef<HTMLDivElement | null>(null);
  

  const [server, setServer] =
    useState<ServerDetailsResponse | null>(
      null
    );

  const [messages, setMessages] =
    useState<MessageResponse[]>([]);

  const [messageText, setMessageText] =
    useState("");
const [unreadMessageCount, setUnreadMessageCount] =
  useState(0);
    const [selectedFiles, setSelectedFiles] =
  useState<File[]>([]);

  const [
    replyingToMessage,
    setReplyingToMessage,
  ] = useState<MessageResponse | null>(
    null
  );

  const [
    editingMessageId,
    setEditingMessageId,
  ] = useState<number | null>(null);

  const [editContent, setEditContent] =
    useState("");

  const [isLoading, setIsLoading] =
    useState(true);

  const [isSending, setIsSending] =
    useState(false);

  const [
    updatingMessageId,
    setUpdatingMessageId,
  ] = useState<number | null>(null);

  const [
    deletingMessageId,
    setDeletingMessageId,
  ] = useState<number | null>(null);

  const [
    isRealtimeConnected,
    setIsRealtimeConnected,
  ] = useState(false);

  const [error, setError] =
    useState<string | null>(null);

  const [sendError, setSendError] =
    useState<string | null>(null);

  const [actionError, setActionError] =
    useState<string | null>(null);

  useEffect(() => {
    const serverId = Number(
      params.serverId
    );

    const channelId = Number(
      params.channelId
    );

    if (!accessToken) {
      setError(
        "İstifadəçi hesabına daxil olunmayıb."
      );

      setIsLoading(false);
      return;
    }

    if (
      !Number.isInteger(serverId) ||
      serverId <= 0 ||
      !Number.isInteger(channelId) ||
      channelId <= 0
    ) {
      setError(
        "Server və ya kanal məlumatı düzgün deyil."
      );

      setIsLoading(false);
      return;
    }

    let isCancelled = false;

    const loadChannel = async () => {
      try {
        setIsLoading(true);
        setError(null);
        setActionError(null);
        setEditingMessageId(null);
        setReplyingToMessage(null);
        setEditContent("");

        const [
          serverResponse,
          messagesResponse,
        ] = await Promise.all([
          getServerById(
            serverId,
            accessToken
          ),

          getChannelMessages(
            channelId,
            accessToken
          ),
        ]);

        const selectedChannel =
          serverResponse.channels.find(
            channel =>
              channel.id === channelId
          );

        if (!selectedChannel) {
          throw new Error(
            "Kanal bu serverdə tapılmadı."
          );
        }

        if (
          selectedChannel.type !==
          TEXT_CHANNEL_TYPE
        ) {
          throw new Error(
            "Bu səhifə yalnız text kanallar üçündür."
          );
        }

        if (!isCancelled) {
          setServer(serverResponse);
          setMessages(messagesResponse);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : "Kanal yüklənə bilmədi."
          );
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    };

    void loadChannel();

    return () => {
      isCancelled = true;
    };
  }, [
    params.serverId,
    params.channelId,
    accessToken,
  ]);

useEffect(() => {
  const channelId = Number(
    params.channelId
  );

  if (
    !connection ||
    !accessToken ||
    !Number.isInteger(channelId) ||
    channelId <= 0
  ) {
    setIsRealtimeConnected(false);
    return;
  }

  if (chatHubStatus !== "connected") {
    setIsRealtimeConnected(false);
    return;
  }

  let isCancelled = false;

 const handleMessageCreated = (
  message: MessageResponse
) => {
  if (message.channelId !== channelId) {
    return;
  }

  const shouldAutoScroll =
    isNearMessagesBottom();

  let messageWasAdded = false;

  setMessages(currentMessages => {
    const alreadyExists =
      currentMessages.some(
        currentMessage =>
          currentMessage.id === message.id
      );

    if (alreadyExists) {
      return currentMessages;
    }

    messageWasAdded = true;

    return [
      ...currentMessages,
      message,
    ];
  });

  window.requestAnimationFrame(() => {
    if (!messageWasAdded) {
      return;
    }

    if (shouldAutoScroll) {
      scrollToLatestMessage("smooth");
      return;
    }

    if (
      message.authorId !==
      Number(currentUser?.id)
    ) {
      setUnreadMessageCount(
        currentCount => currentCount + 1
      );
    }
  });
};

  const handleMessageUpdated = (
  message: MessageResponse
) => {
  if (
    message.channelId !== channelId
  ) {
    return;
  }

  setMessages(currentMessages =>
    currentMessages.map(
      currentMessage =>
        currentMessage.id ===
        message.id
          ? message
          : currentMessage
    )
  );

  setReplyingToMessage(
    currentMessage =>
      currentMessage?.id ===
      message.id
        ? message
        : currentMessage
  );
};
 (
    message: MessageResponse
  ) => {
    if (
      message.channelId !== channelId
    ) {
      return;
    }

    setMessages(currentMessages =>
      currentMessages.map(
        currentMessage =>
          currentMessage.id ===
          message.id
            ? message
            : currentMessage
      )
    );

    setReplyingToMessage(
      currentMessage =>
        currentMessage?.id ===
        message.id
          ? message
          : currentMessage
    );
  };

  const handleMessageDeleted = (
    deletedChannelId: number,
    messageId: number
  ) => {
    if (
      deletedChannelId !== channelId
    ) {
      return;
    }

    setMessages(currentMessages =>
      currentMessages.filter(
        message =>
          message.id !== messageId
      )
    );

    setEditingMessageId(
      currentMessageId =>
        currentMessageId === messageId
          ? null
          : currentMessageId
    );

    setReplyingToMessage(
      currentMessage =>
        currentMessage?.id ===
        messageId
          ? null
          : currentMessage
    );
  };

  connection.on(
    "MessageCreated",
    handleMessageCreated
  );

  connection.on(
    "MessageUpdated",
    handleMessageUpdated
  );

  connection.on(
    "MessageDeleted",
    handleMessageDeleted
  );

  const joinChannel = async () => {
    try {
      await connection.invoke(
        "JoinChannel",
        channelId
      );

      if (!isCancelled) {
        setIsRealtimeConnected(true);
      }
    } catch (connectionError) {
      if (!isCancelled) {
        setIsRealtimeConnected(false);

        console.error(
          "Channel could not be joined:",
          connectionError
        );
      }
    }
  };

  void joinChannel();

  return () => {
    isCancelled = true;

    connection.off(
      "MessageCreated",
      handleMessageCreated
    );

    connection.off(
      "MessageUpdated",
      handleMessageUpdated
    );

    connection.off(
      "MessageDeleted",
      handleMessageDeleted
    );

    setIsRealtimeConnected(false);

    if (
      connection.state ===
      HubConnectionState.Connected
    ) {
      void connection
        .invoke(
          "LeaveChannel",
          channelId
        )
        .catch(leaveError => {
          console.error(
            "Channel could not be left:",
            leaveError
          );
        });
    }

    /*
     * Qlobal bağlantı olduğu üçün
     * connection.stop() çağırılmır.
     */
  };
}, [
  params.channelId,
  accessToken,
  connection,
  chatHubStatus,
]);

const isNearMessagesBottom = () => {
  const container =
    messagesContainerRef.current;

  if (!container) {
    return true;
  }

  const distanceFromBottom =
    container.scrollHeight -
    container.scrollTop -
    container.clientHeight;

  return distanceFromBottom <= 100;
};

const scrollToLatestMessage = (
  behavior: ScrollBehavior = "smooth"
) => {
  const container =
    messagesContainerRef.current;

  if (!container) {
    return;
  }

  container.scrollTo({
    top: container.scrollHeight,
    behavior,
  });

  setUnreadMessageCount(0);
};

const handleMessagesScroll = () => {
  if (isNearMessagesBottom()) {
    setUnreadMessageCount(0);
  }
};

const handleFileChange = (
  event: ChangeEvent<HTMLInputElement>
) => {
  const files = Array.from(
    event.target.files ?? []
  );

  if (files.length === 0) {
    return;
  }

  setSelectedFiles(currentFiles => [
    ...currentFiles,
    ...files,
  ]);

  event.target.value = "";
};

const removeSelectedFile = (
  fileIndex: number
) => {
  setSelectedFiles(currentFiles =>
    currentFiles.filter(
      (_, index) => index !== fileIndex
    )
  );
};

  const handleSendMessage = async (
  event: FormEvent<HTMLFormElement>
) => {
  event.preventDefault();

  const content = messageText.trim();

  const channelId = Number(
    params.channelId
  );

  if (
    (!content && selectedFiles.length === 0) ||
    !accessToken ||
    isSending
  ) {
    return;
  }

  try {
    setIsSending(true);
    setSendError(null);

    const createdMessage =
      await createMessage(
        channelId,
        {
          content,
          replyToMessageId:
            replyingToMessage?.id ?? null,
          attachments: selectedFiles,
        },
        accessToken
      );

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
    setSelectedFiles([]);
    setReplyingToMessage(null);

    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
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

  const startReplying = (
    message: MessageResponse
  ) => {
    setReplyingToMessage(message);
    setEditingMessageId(null);
    setEditContent("");
    setActionError(null);

    window.setTimeout(() => {
      messageInputRef.current?.focus();
    }, 0);
  };

  const cancelReplying = () => {
    setReplyingToMessage(null);
  };

  const startEditing = (
    message: MessageResponse
  ) => {
    setEditingMessageId(message.id);
    setEditContent(message.content);
    setReplyingToMessage(null);
    setActionError(null);
  };

  const cancelEditing = () => {
    setEditingMessageId(null);
    setEditContent("");
    setActionError(null);
  };

  const handleUpdateMessage = async (
    event: FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault();

    const channelId = Number(
      params.channelId
    );

    const content =
      editContent.trim();

    if (
      !accessToken ||
      !editingMessageId ||
      !content ||
      updatingMessageId !== null
    ) {
      return;
    }

    try {
      setUpdatingMessageId(
        editingMessageId
      );

      setActionError(null);

      const updatedMessage =
        await updateMessage(
          channelId,
          editingMessageId,
          {
            content,
          },
          accessToken
        );

      setMessages(currentMessages =>
        currentMessages.map(
          message =>
            message.id ===
            updatedMessage.id
              ? updatedMessage
              : message
        )
      );

      setEditingMessageId(null);
      setEditContent("");
    } catch (updateError) {
      setActionError(
        updateError instanceof Error
          ? updateError.message
          : "Mesaj redaktə edilə bilmədi."
      );
    } finally {
      setUpdatingMessageId(null);
    }
  };

  const handleDeleteMessage = async (
    messageId: number
  ) => {
    const channelId = Number(
      params.channelId
    );

    if (
      !accessToken ||
      deletingMessageId !== null
    ) {
      return;
    }

    const confirmed = window.confirm(
      "Bu mesajı silmək istədiyinizə əminsiniz?"
    );

    if (!confirmed) {
      return;
    }

    try {
      setDeletingMessageId(messageId);
      setActionError(null);

      await deleteMessage(
        channelId,
        messageId,
        accessToken
      );

      setMessages(currentMessages =>
        currentMessages.filter(
          message =>
            message.id !== messageId
        )
      );

      if (
        editingMessageId === messageId
      ) {
        setEditingMessageId(null);
        setEditContent("");
      }

      if (
        replyingToMessage?.id ===
        messageId
      ) {
        setReplyingToMessage(null);
      }
    } catch (deleteError) {
      setActionError(
        deleteError instanceof Error
          ? deleteError.message
          : "Mesaj silinə bilmədi."
      );
    } finally {
      setDeletingMessageId(null);
    }
  };

  if (isLoading) {
    return (
      <main className="ml-[88px] flex min-h-screen items-center justify-center bg-background text-gray-300">
        Mesajlar yüklənir...
      </main>
    );
  }

  const channelId = Number(
    params.channelId
  );

  const selectedChannel =
    server?.channels.find(
      channel =>
        channel.id === channelId
    );

  if (
    error ||
    !server ||
    !selectedChannel
  ) {
    return (
      <main className="ml-[88px] flex min-h-screen items-center justify-center bg-background">
        <div className="rounded-lg bg-red-950/40 px-6 py-4 text-red-300">
          {error ??
            "Kanal tapılmadı."}
        </div>
      </main>
    );
  }

  const currentUserId = Number(
    currentUser?.id
  );

  return (
    <main className="ml-[88px] flex h-screen bg-background text-white">
      <aside className="flex w-[320px] flex-col border-r border-black/20 bg-semibackground">
        <div className="border-b border-black/20 px-5 py-5">
          <Link
            href={`/servers/${server.id}`}
            className="text-sm text-gray-400 hover:text-white"
          >
            ← Serverə qayıt
          </Link>

          <h1 className="mt-5 truncate text-lg font-semibold">
            {server.name}
          </h1>
        </div>

        <div className="px-4 py-5">
          <div className="rounded bg-white/10 px-3 py-3 font-medium">
            # {selectedChannel.name}
          </div>

          {selectedChannel.topic && (
            <p className="mt-4 text-sm text-gray-400">
              {selectedChannel.topic}
            </p>
          )}
        </div>
      </aside>

      <section className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-[72px] items-center border-b border-black/20 px-6 shadow">
          <span className="mr-3 text-2xl text-gray-400">
            #
          </span>

          <h2 className="font-semibold">
            {selectedChannel.name}
          </h2>

          {selectedChannel.topic && (
            <>
              <div className="mx-5 h-6 w-px bg-gray-600" />

              <p className="truncate text-sm text-gray-400">
                {selectedChannel.topic}
              </p>
            </>
          )}

          <div className="ml-auto flex items-center gap-2 text-xs text-gray-400">
            <span
              className={`h-2 w-2 rounded-full ${
                isRealtimeConnected
                  ? "bg-green-500"
                  : "bg-red-500"
              }`}
            />

            {isRealtimeConnected
              ? "Real-time bağlıdır"
              : "Real-time ayrılıb"}
          </div>
        </header>

      <div
  ref={messagesContainerRef}
  onScroll={handleMessagesScroll}
  className="flex-1 overflow-y-auto px-6 py-5"
>
          {actionError && (
            <div className="mb-4 rounded-md bg-red-950/40 px-4 py-3 text-sm text-red-300">
              {actionError}
            </div>
          )}

          {messages.length === 0 ? (
            <div className="flex h-full items-center justify-center text-gray-500">
              Bu kanalda hələ mesaj yoxdur.
            </div>
          ) : (
            <div className="space-y-3">
              {messages.map(message => {
                const avatarUrl =
                  getAvatarUrl(
                    message.authorAvatarUrl
                  );

                const authorInitial =
                  message.authorDisplayName
                    .charAt(0)
                    .toUpperCase();

                const isAuthor =
                  currentUserId ===
                  message.authorId;

                const canDelete =
                  isAuthor ||
                  currentUserId ===
                    server.ownerId;

                const isEditing =
                  editingMessageId ===
                  message.id;

                const repliedMessage =
                  message.replyToMessageId
                    ? messages.find(
                        currentMessage =>
                          currentMessage.id ===
                          message.replyToMessageId
                      )
                    : null;

                return (
                  <article
                    key={message.id}
                    className="group flex gap-3 rounded-md px-2 py-2 hover:bg-white/[0.03]"
                  >
                    <div
                      className="flex h-11 w-11 flex-none items-center justify-center rounded-full bg-primary bg-cover bg-center font-semibold"
                      style={
                        avatarUrl
                          ? {
                              backgroundImage:
                                `url("${avatarUrl}")`,
                            }
                          : undefined
                      }
                    >
                      {!avatarUrl &&
                        authorInitial}
                    </div>

                    <div className="min-w-0 flex-1">
                      {message.replyToMessageId && (
                        <div className="mb-1 truncate text-xs text-gray-500">
                          ↪{" "}
                          {repliedMessage
                            ? `${repliedMessage.authorDisplayName}: ${repliedMessage.content}`
                            : `Mesaj #${message.replyToMessageId}`}
                        </div>
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

                        {message.isPinned && (
                          <span className="text-xs text-yellow-400">
                            📌
                          </span>
                        )}

                        {!isEditing && (
                          <div className="ml-auto flex items-center gap-2">
                            <button
                              type="button"
                              onClick={() =>
                                startReplying(
                                  message
                                )
                              }
                              className="rounded px-2 py-1 text-xs text-gray-400 hover:bg-white/10 hover:text-white"
                            >
                              Cavab ver
                            </button>

                            {isAuthor && (
                              <button
                                type="button"
                                onClick={() =>
                                  startEditing(
                                    message
                                  )
                                }
                                className="rounded px-2 py-1 text-xs text-gray-400 hover:bg-white/10 hover:text-white"
                              >
                                Redaktə et
                              </button>
                            )}

                            {canDelete && (
                              <button
                                type="button"
                                onClick={() =>
                                  void handleDeleteMessage(
                                    message.id
                                  )
                                }
                                disabled={
                                  deletingMessageId ===
                                  message.id
                                }
                                className="rounded px-2 py-1 text-xs text-red-400 hover:bg-red-950/40 disabled:opacity-50"
                              >
                                {deletingMessageId ===
                                message.id
                                  ? "Silinir..."
                                  : "Sil"}
                              </button>
                            )}
                          </div>
                        )}
                      </div>

                      {isEditing ? (
                        <form
                          onSubmit={
                            handleUpdateMessage
                          }
                          className="mt-2"
                        >
                          <input
                            autoFocus
                            type="text"
                            value={
                              editContent
                            }
                            onChange={event =>
                              setEditContent(
                                event.target
                                  .value
                              )
                            }
                            maxLength={2000}
                            disabled={
                              updatingMessageId ===
                              message.id
                            }
                            className="w-full rounded-md bg-white/10 px-3 py-2 text-white outline-none focus:ring-1 focus:ring-primary"
                          />

                          <div className="mt-2 flex items-center gap-2">
                            <button
                              type="submit"
                              disabled={
                                !editContent.trim() ||
                                updatingMessageId ===
                                  message.id
                              }
                              className="rounded bg-primary px-3 py-1.5 text-xs font-semibold disabled:opacity-40"
                            >
                              {updatingMessageId ===
                              message.id
                                ? "Yadda saxlanılır..."
                                : "Yadda saxla"}
                            </button>

                            <button
                              type="button"
                              onClick={
                                cancelEditing
                              }
                              disabled={
                                updatingMessageId ===
                                message.id
                              }
                              className="rounded px-3 py-1.5 text-xs text-gray-400 hover:bg-white/10"
                            >
                              Ləğv et
                            </button>
                          </div>
                        </form>
                      ) : (
  <>
    {message.content && (
      <p className="mt-1 whitespace-pre-wrap break-words text-gray-100">
        {message.content}
      </p>
    )}

    {message.attachments.length > 0 && (
     <div
  className={
    message.attachments.length === 1
      ? "mt-2 w-fit max-w-[550px]"
      : "mt-2 grid w-full max-w-[550px] grid-cols-2 gap-1 overflow-hidden rounded-lg"
  }
>
        {message.attachments.map(
          (attachment, index) => {
            const attachmentUrl =
              attachment.fileUrl.startsWith(
                "http://"
              ) ||
              attachment.fileUrl.startsWith(
                "https://"
              )
                ? attachment.fileUrl
                : `${API_URL}${
                    attachment.fileUrl.startsWith(
                      "/"
                    )
                      ? ""
                      : "/"
                  }${attachment.fileUrl}`;

            const isImage =
              attachment.contentType.startsWith(
                "image/"
              );
              const isSingle =
  message.attachments.length === 1;

const isLastOdd =
  message.attachments.length > 1 &&
  message.attachments.length % 2 !== 0 &&
  index === message.attachments.length - 1;

            return isImage ? (
            <a
  key={attachment.id}
  href={attachmentUrl}
  target="_blank"
  rel="noopener noreferrer"
  className={
    isSingle
      ? "block w-fit overflow-hidden rounded-lg"
      : `block h-[240px] min-w-0 overflow-hidden bg-black/20 ${
          isLastOdd ? "col-span-2" : ""
        }`
  }
>
  <img
    src={attachmentUrl}
    alt={attachment.fileName}
    className={
      isSingle
        ? "block max-h-[350px] max-w-[550px] rounded-lg object-contain"
        : "h-full w-full object-cover"
    }
  />
</a>
            ) : (
              <a
                key={attachment.id}
                href={attachmentUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="rounded-md bg-white/10 px-3 py-2 text-sm text-blue-300 hover:underline"
              >
                {attachment.fileName}
              </a>
            );
          }
        )}
      </div>
    )}
  </>
)}
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </div>

        <div className="px-6 pb-6">
          {sendError && (
            <p className="mb-2 text-sm text-red-400">
              {sendError}
            </p>
          )}
{unreadMessageCount > 0 && (
  <button
    type="button"
    onClick={() =>
      scrollToLatestMessage("smooth")
    }
    className="mx-4 mb-2 rounded-md bg-primary px-4 py-2 text-sm font-semibold text-white shadow-lg hover:brightness-110"
  >
    {unreadMessageCount} yeni mesaj ↓
  </button>
)}
          {replyingToMessage && (
            <div className="flex items-center justify-between rounded-t-lg border-b border-black/20 bg-white/10 px-4 py-2">
              <div className="min-w-0">
                <p className="text-xs text-gray-400">
                  Cavab verilir:
                  <span className="ml-1 font-semibold text-gray-200">
                    {
                      replyingToMessage.authorDisplayName
                    }
                  </span>
                </p>

                <p className="truncate text-sm text-gray-400">
                  {replyingToMessage.content}
                </p>
              </div>

              <button
                type="button"
                onClick={cancelReplying}
                className="ml-4 rounded px-2 py-1 text-gray-400 hover:bg-white/10 hover:text-white"
                aria-label="Cavabı ləğv et"
              >
                ✕
              </button>
            </div>
          )}
{selectedFiles.length > 0 && (
  <div className="flex flex-wrap gap-2 border-b border-black/20 bg-white/10 px-4 py-3">
    {selectedFiles.map((file, index) => (
      <div
        key={`${file.name}-${file.lastModified}-${index}`}
        className="flex max-w-[220px] items-center gap-2 rounded-md bg-background/70 px-3 py-2"
      >
        <div className="min-w-0">
          <p className="truncate text-sm text-gray-200">
            {file.name}
          </p>

          <p className="text-xs text-gray-500">
            {(file.size / 1024).toFixed(1)} KB
          </p>
        </div>

        <button
          type="button"
          onClick={() =>
            removeSelectedFile(index)
          }
          disabled={isSending}
          className="ml-auto rounded px-1.5 py-1 text-gray-400 hover:bg-white/10 hover:text-white disabled:opacity-50"
          aria-label={`${file.name} faylını sil`}
          title="Seçilmiş faylı sil"
        >
          ✕
        </button>
      </div>
    ))}
  </div>
)}
          <form
            onSubmit={handleSendMessage}
            className={`flex items-center bg-white/10 ${
              replyingToMessage ||
selectedFiles.length > 0
                ? "rounded-b-lg"
                : "rounded-lg"
            }`}
          >
          <input
  ref={fileInputRef}
  type="file"
  multiple
  accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
  onChange={handleFileChange}
  disabled={isSending}
  className="hidden"
/>

<button
  type="button"
  onClick={() =>
    fileInputRef.current?.click()
  }
  disabled={isSending}
  className="ml-3 flex h-9 w-9 flex-none items-center justify-center rounded-full bg-gray-500 text-xl font-semibold text-background hover:bg-gray-300 disabled:cursor-not-allowed disabled:opacity-50"
  aria-label="Fayl əlavə et"
  title="Fayl əlavə et"
>
  +
</button>
            <input
              ref={messageInputRef}
              type="text"
              value={messageText}
              onChange={event =>
                setMessageText(
                  event.target.value
                )
              }
              placeholder={`#${selectedChannel.name} kanalına mesaj yaz`}
              disabled={isSending}
              maxLength={2000}
              className="min-w-0 flex-1 bg-transparent px-5 py-4 text-white outline-none placeholder:text-gray-500 disabled:opacity-60"
            />

            <button
              type="submit"
              disabled={
  (!messageText.trim() &&
    selectedFiles.length === 0) ||
  isSending
}
              className="mr-2 rounded-md bg-primary px-4 py-2 text-sm font-semibold hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-40"
            >
              {isSending
                ? "Göndərilir..."
                : "Göndər"}
            </button>
          </form>
        </div>
      </section>
    </main>
  );
}