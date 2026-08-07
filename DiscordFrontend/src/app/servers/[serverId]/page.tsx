"use client";

import {
  useCallback,
  useEffect,
  useState,
} from "react";
import Link from "next/link";
import {
  useParams,
  useRouter,
} from "next/navigation";
import VoiceStatusFooter from "@/components/islets/voice-status-footer";
import {
  ChannelResponse,
  getServerById,
  getServerMembers,
  leaveServer,
  ServerDetailsResponse,
  ServerMemberResponse,
  ServerResponse,
  deleteChannel,
updateChannel,
createChannel,
} from "@/lib/serverApi";
import {
  ServerPermission,
} from "@/lib/serverPermissions";
import { useAuthStore } from "@/state/auth";

import {
  useVoice,
} from "@/components/voice/voice-provider";

import { BsGearFill } from "react-icons/bs";

import ServerSettingsModal from "@/components/islets/server-settings-modal";
import ServerMemberList from "@/components/islets/server-member-list";
import { useCurrentUserStore } from "@/state/user";
import { isApiError } from "@/lib/api";

const CHANNEL_TYPE = {
  Text: 0,
  Voice: 1,
  Category: 2,
} as const;

function ChannelItem({
  channel,
  serverId,
  canManageChannels,
  onManageChannel,
}: {
  channel: ChannelResponse;
  serverId: number;
  canManageChannels: boolean;
  onManageChannel: (
    channel: ChannelResponse
  ) => void;
}) {
  const {
    activeChannelId,
    participants,
    status,
    joinVoiceChannel,
    leaveVoiceChannel,
  } = useVoice();

  const [isChangingVoice, setIsChangingVoice] =
    useState(false);

  const [voiceError, setVoiceError] =
    useState<string | null>(null);

    const [
  isDeleteChannelConfirmOpen,
  setIsDeleteChannelConfirmOpen,
] = useState(false);
const [
  isCreateCategoryOpen,
  setIsCreateCategoryOpen,
] = useState(false);

const [
  newCategoryName,
  setNewCategoryName,
] = useState("");

const [
  isCreatingCategory,
  setIsCreatingCategory,
] = useState(false);

const [
  createCategoryError,
  setCreateCategoryError,
] = useState<string | null>(null);
  const className =
    "flex w-full items-center gap-2 rounded px-2 py-1.5 text-left text-sm text-gray-400 hover:bg-white/5 hover:text-gray-200";

  if (channel.type === CHANNEL_TYPE.Text) {
  return (
    <div className="group flex items-center">
      <Link
        href={`/servers/${serverId}/channels/${channel.id}`}
        className={`${className} min-w-0 flex-1`}
      >
        <span className="w-5 text-center text-lg">
          #
        </span>

        <span className="truncate">
          {channel.name}
        </span>
      </Link>

      {canManageChannels && (
        <button
          type="button"
          onClick={() =>
    onManageChannel(channel)
  }
          aria-label={`Manage ${channel.name}`}
          title="Edit Channel"
          className="hidden h-7 w-7 flex-none items-center justify-center rounded text-gray-400 hover:bg-white/10 hover:text-white group-hover:flex"
        >
          <BsGearFill fontSize={13} />
        </button>
      )}
    </div>
  );
}

  if (channel.type !== CHANNEL_TYPE.Voice) {
    return null;
  }

  const isActive =
    activeChannelId === channel.id;

  const handleVoiceClick = async () => {
    if (isChangingVoice) {
      return;
    }

    try {
      setIsChangingVoice(true);
      setVoiceError(null);

      if (isActive) {
        await leaveVoiceChannel();
      } else {
        await joinVoiceChannel(channel.id);
      }
    } catch (error) {
      setVoiceError(
        error instanceof Error
          ? error.message
          : "Voice kanalına qoşulmaq alınmadı."
      );
    } finally {
      setIsChangingVoice(false);
    }
  };

  const statusText =
    status === "connecting"
      ? "Qoşulur..."
      : status === "connected"
        ? "Voice bağlıdır"
        : status === "reconnecting"
          ? "Yenidən qoşulur..."
          : "Bağlantı kəsilib";

  return (
    <div className="mb-1">
      <button
        type="button"
        onClick={handleVoiceClick}
        disabled={isChangingVoice}
        className={`${className} ${
          isActive
            ? "bg-white/10 text-green-300"
            : ""
        } disabled:cursor-wait disabled:opacity-60`}
      >
        <span className="w-5 text-center text-lg">
          🔊
        </span>

        <span className="min-w-0 flex-1 truncate">
          {channel.name}
        </span>

        {isActive && (
          <span className="h-2 w-2 flex-none rounded-full bg-green-500" />
        )}
      </button>

      {isActive && (
        <div className="ml-9 mt-1 space-y-1">
          <p className="text-[11px] text-green-400">
            {statusText}
          </p>

          {participants.map(participant => (
            <div
              key={participant.userId}
              className="flex items-center gap-2 py-0.5 text-xs text-gray-300"
            >
              <span className="h-2 w-2 flex-none rounded-full bg-green-500" />

              <span className="truncate">
                {participant.displayName}
              </span>
            </div>
          ))}

          {status === "connected" &&
            participants.length === 0 && (
              <p className="text-xs text-gray-500">
                İştirakçılar gözlənilir...
              </p>
            )}
        </div>
      )}

      {voiceError && (
        <p className="ml-9 mt-1 text-xs text-red-400">
          {voiceError}
        </p>
      )}
    </div>
  );
}

export default function ServerPage() {
  const params = useParams<{
    serverId: string;
  }>();
const router = useRouter();
  const accessToken = useAuthStore(
    state => state.accessToken
  );

  const currentUser = useCurrentUserStore(
  state => state.currentUser
);

  const [server, setServer] =
    useState<ServerDetailsResponse | null>(null);
const [serverMembers, setServerMembers] =
  useState<ServerMemberResponse[]>([]);

  const [
  channelToManage,
  setChannelToManage,
] = useState<ChannelResponse | null>(null);
const [
  isCreateChannelOpen,
  setIsCreateChannelOpen,
] = useState(false);
const [
  isCreateCategoryOpen,
  setIsCreateCategoryOpen,
] = useState(false);

const [
  newCategoryName,
  setNewCategoryName,
] = useState("");

const [
  isCreatingCategory,
  setIsCreatingCategory,
] = useState(false);

const [
  createCategoryError,
  setCreateCategoryError,
] = useState<string | null>(null);
const [
  newChannelName,
  setNewChannelName,
] = useState("");

const [
  newChannelType,
  setNewChannelType,
] = useState<number>(
  CHANNEL_TYPE.Text
);

const [
  newChannelParentCategoryId,
  setNewChannelParentCategoryId,
] = useState<number | null>(null);

const [
  isCreatingChannel,
  setIsCreatingChannel,
] = useState(false);

const [
  createChannelError,
  setCreateChannelError,
] = useState<string | null>(null);


const [
  isSavingChannel,
  setIsSavingChannel,
] = useState(false);
const [
  isChannelSaved,
  setIsChannelSaved,
] = useState(false);
const [
  isDeletingChannel,
  setIsDeletingChannel,
] = useState(false);
const [
  isDeleteChannelConfirmOpen,
  setIsDeleteChannelConfirmOpen,
] = useState(false);

const [
  isServerMenuOpen,
  setIsServerMenuOpen,
] = useState(false);
const [
  channelManageError,
  setChannelManageError,
] = useState<string | null>(null);
  const [isLoading, setIsLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(null);
const [
  isSettingsOpen,
  setIsSettingsOpen,
] = useState(false);

const [isLeaving, setIsLeaving] =
  useState(false);

  const redirectFromServer =
  useCallback(
    (message?: string) => {
      window.dispatchEvent(
        new Event("servers:refresh")
      );

      if (message) {
        window.alert(message);
      }

      router.replace("/channels/me");
    },
    [router]
  );
  useEffect(() => {
    const serverId = Number(params.serverId);

    if (
      !accessToken ||
      !Number.isInteger(serverId) ||
      serverId <= 0
    ) {
      setError(
        "Server məlumatı düzgün deyil."
      );

      setIsLoading(false);
      return;
    }

    let isCancelled = false;

    const loadServer = async () => {
      try {
        setIsLoading(true);
        setError(null);

const [
  serverResponse,
  membersResponse,
] = await Promise.all([
  getServerById(
    serverId,
    accessToken
  ),

  getServerMembers(
    serverId,
    accessToken
  ),
]);

       if (!isCancelled) {
  setServer(serverResponse);
  setServerMembers(membersResponse);
}
      } catch (loadError) {
  if (isCancelled) {
    return;
  }

  if (
    isApiError(loadError) &&
    (
      loadError.status === 403 ||
      loadError.status === 404
    )
  ) {
    redirectFromServer(
      "Artıq bu serverə giriş icazəniz yoxdur."
    );

    return;
  }

  setError(
    loadError instanceof Error
      ? loadError.message
      : "Server yüklənə bilmədi."
  );
} finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    };

    loadServer();

    return () => {
      isCancelled = true;
    };
  }, [
    params.serverId,
    accessToken,
    redirectFromServer,
  ]);

  useEffect(() => {
  const currentServerId =
    Number(params.serverId);

  const handleServerMemberRemoved =
    (event: Event) => {
      const removedEvent =
        event as CustomEvent<{
          serverId: number;
          message: string;
        }>;

      if (
        removedEvent.detail.serverId !==
        currentServerId
      ) {
        return;
      }

      redirectFromServer(
        removedEvent.detail.message
      );
    };

  window.addEventListener(
    "server-membership:removed",
    handleServerMemberRemoved
  );

  return () => {
    window.removeEventListener(
      "server-membership:removed",
      handleServerMemberRemoved
    );
  };
}, [
  params.serverId,
  redirectFromServer,
]);
const handleServerUpdated = (
  updatedServer: ServerResponse
) => {
  setServer(currentServer =>
    currentServer
      ? {
          ...currentServer,
          ...updatedServer,
        }
      : currentServer
  );

  window.dispatchEvent(
    new Event("servers:refresh")
  );
};

useEffect(() => {
  let isDisposed = false;

  const handleServerMembersChanged =
    async (event: Event) => {
      const membersEvent =
        event as CustomEvent<{
          serverId: number;
        }>;

      const currentServerId =
        Number(params.serverId);

      if (
        membersEvent.detail.serverId !==
          currentServerId ||
        !accessToken
      ) {
        return;
      }

      try {
        const updatedServer =
          await getServerById(
            currentServerId,
            accessToken
          );

        if (!isDisposed) {
          setServer(updatedServer);
        }
      } catch (refreshError) {
  if (
    isApiError(refreshError) &&
    (
      refreshError.status === 403 ||
      refreshError.status === 404
    )
  ) {
    redirectFromServer(
      "Artıq bu serverə giriş icazəniz yoxdur."
    );

    return;
  }

  console.error(
    "Server member count could not be refreshed:",
    refreshError
  );
}
    };

  window.addEventListener(
    "server-members:changed",
    handleServerMembersChanged
  );

  return () => {
    isDisposed = true;

    window.removeEventListener(
      "server-members:changed",
      handleServerMembersChanged
    );
  };
}, [
  params.serverId,
  accessToken,
  redirectFromServer,
]);

useEffect(() => {
  let isDisposed = false;

  const handleServerChannelsChanged =
    async (event: Event) => {
      const channelsEvent =
        event as CustomEvent<{
          serverId: number;
        }>;

      const currentServerId =
        Number(params.serverId);

      if (
        channelsEvent.detail.serverId !==
          currentServerId ||
        !accessToken
      ) {
        return;
      }

      try {
        const updatedServer =
          await getServerById(
            currentServerId,
            accessToken
          );

        if (!isDisposed) {
          setServer(updatedServer);
        }
      } catch (refreshError) {
        if (
          isApiError(refreshError) &&
          (
            refreshError.status === 403 ||
            refreshError.status === 404
          )
        ) {
          redirectFromServer(
            "Artıq bu serverə giriş icazəniz yoxdur."
          );

          return;
        }

        console.error(
          "Server channels could not be refreshed:",
          refreshError
        );
      }
    };

  window.addEventListener(
    "server-channels:changed",
    handleServerChannelsChanged
  );

  return () => {
    isDisposed = true;

    window.removeEventListener(
      "server-channels:changed",
      handleServerChannelsChanged
    );
  };
}, [
  params.serverId,
  accessToken,
  redirectFromServer,
]);

  if (isLoading) {
    return (
      <main className="ml-[88px] flex min-h-screen items-center justify-center bg-background text-gray-300">
        Server yüklənir...
      </main>
    );
  }

  if (error || !server) {
    return (
      <main className="ml-[88px] flex min-h-screen items-center justify-center bg-background">
        <div className="rounded-lg bg-red-950/40 px-6 py-4 text-red-300">
          {error ?? "Server tapılmadı."}
        </div>
      </main>
    );
  }

  const currentUserId = Number(
  currentUser?.id
);

const currentUserMember =
  serverMembers.find(
    member =>
      member.userId === currentUserId
  ) ?? null;

const canManageChannels =
  currentUserId === server.ownerId ||
  currentUserMember?.roles.some(role =>
    role.permissions.includes(
      ServerPermission.Administrator
    ) ||
    role.permissions.includes(
      ServerPermission.ManageChannels
    )
  ) === true;
const isOwner =
  server.ownerId.toString() ===
  currentUser?.id;

const handleSaveChannel = async () => {
  if (
    !channelToManage ||
    !accessToken ||
    isSavingChannel
  ) {
    return;
  }

  const normalizedName =
    channelToManage.name.trim();

  if (!normalizedName) {
    setChannelManageError(
      "Channel name is required."
    );
    return;
  }

  try {
    setIsSavingChannel(true);
    setChannelManageError(null);
setIsChannelSaved(false);
    const updatedChannel =
      await updateChannel(
        server.id,
        channelToManage.id,
        {
          name: normalizedName,
          topic:
            channelToManage.topic?.trim() ||
            null,
          isPrivate:
            channelToManage.isPrivate,
          parentCategoryId:
            channelToManage.parentCategoryId,
          bitrate:
            channelToManage.bitrate,
          userLimit:
            channelToManage.userLimit,
        },
        accessToken
      );

    setServer(currentServer => {
      if (!currentServer) {
        return currentServer;
      }

      return {
        ...currentServer,
        channels:
          currentServer.channels.map(
            channel =>
              channel.id ===
              updatedChannel.id
                ? updatedChannel
                : channel
          ),
      };
    });

    setChannelToManage(
      updatedChannel
    );
setIsChannelSaved(true);

  } catch (saveError) {
    setChannelManageError(
      saveError instanceof Error
        ? saveError.message
        : "Channel could not be saved."
    );
  } finally {
    setIsSavingChannel(false);
  }
};

const handleDeleteChannel = async () => {
  if (
    !channelToManage ||
    !accessToken ||
    isDeletingChannel
  ) {
    return;
  }

  if (!isDeleteChannelConfirmOpen) {
    setIsDeleteChannelConfirmOpen(true);
    return;
  }


  const channelId =
    channelToManage.id;

  try {
    setIsDeletingChannel(true);
    setChannelManageError(null);

    await deleteChannel(
      server.id,
      channelId,
      accessToken
    );

    setServer(currentServer => {
      if (!currentServer) {
        return currentServer;
      }

      return {
        ...currentServer,
        channels:
          currentServer.channels.filter(
            channel =>
              channel.id !== channelId
          ),
      };
    });
setIsDeleteChannelConfirmOpen(false);
    setChannelToManage(null);
  } catch (deleteError) {
    setChannelManageError(
      deleteError instanceof Error
        ? deleteError.message
        : "Channel could not be deleted."
    );
  } finally {
    setIsDeletingChannel(false);
  }
};

const handleCreateChannel = async () => {
  if (
    !accessToken ||
    !server ||
    isCreatingChannel
  ) {
    return;
  }

  const normalizedName =
    newChannelName.trim();

  if (!normalizedName) {
    setCreateChannelError(
      "Channel name is required."
    );
    return;
  }

  try {
    setIsCreatingChannel(true);
    setCreateChannelError(null);

    const createdChannel =
      await createChannel(
        server.id,
        {
          name: normalizedName,
          topic: null,
          type: newChannelType,
          isPrivate: false,
          parentCategoryId:
            newChannelType ===
            CHANNEL_TYPE.Category
              ? null
              : newChannelParentCategoryId,
          bitrate: null,
          userLimit: null,
        },
        accessToken
      );

    setServer(currentServer => {
      if (!currentServer) {
        return currentServer;
      }

      return {
        ...currentServer,
        channels: [
          ...currentServer.channels,
          createdChannel,
        ],
      };
    });

    setNewChannelName("");
    setNewChannelType(
      CHANNEL_TYPE.Text
    );
    setNewChannelParentCategoryId(null);
    setIsCreateChannelOpen(false);
  } catch (createError) {
    setCreateChannelError(
      createError instanceof Error
        ? createError.message
        : "Channel could not be created."
    );
  } finally {
    setIsCreatingChannel(false);
  }
};

const handleCreateCategory = async () => {
  if (
    !accessToken ||
    !server ||
    isCreatingCategory
  ) {
    return;
  }

  const normalizedName =
    newCategoryName.trim();

  if (!normalizedName) {
    setCreateCategoryError(
      "Category name is required."
    );
    return;
  }

  try {
    setIsCreatingCategory(true);
    setCreateCategoryError(null);

    const createdCategory =
      await createChannel(
        server.id,
        {
          name: normalizedName,
          topic: null,
          type: CHANNEL_TYPE.Category,
          isPrivate: false,
          parentCategoryId: null,
          bitrate: null,
          userLimit: null,
        },
        accessToken
      );

    setServer(currentServer => {
      if (!currentServer) {
        return currentServer;
      }

      return {
        ...currentServer,
        channels: [
          ...currentServer.channels,
          createdCategory,
        ],
      };
    });

    setNewCategoryName("");
    setIsCreateCategoryOpen(false);
  } catch (createError) {
    setCreateCategoryError(
      createError instanceof Error
        ? createError.message
        : "Category could not be created."
    );
  } finally {
    setIsCreatingCategory(false);
  }
};
  const handleLeaveServer = async () => {
  if (
    !server ||
    !accessToken ||
    isOwner ||
    isLeaving
  ) {
    return;
  }

  const shouldLeave =
    window.confirm(
      `Leave ${server.name}?`
    );

  if (!shouldLeave) {
    return;
  }

  try {
    setIsLeaving(true);

    await leaveServer(
      server.id,
      accessToken
    );

    window.dispatchEvent(
      new Event("servers:refresh")
    );

    router.push("/channels/me");
  } catch (leaveError) {
    window.alert(
      leaveError instanceof Error
        ? leaveError.message
        : "Server could not be left."
    );
  } finally {
    setIsLeaving(false);
  }
};
  const categories =
    server.channels
      .filter(
        channel =>
          channel.type ===
          CHANNEL_TYPE.Category
      )
      .sort(
        (first, second) =>
          first.position -
          second.position
      );

  const uncategorizedChannels =
    server.channels
      .filter(
        channel =>
          channel.type !==
            CHANNEL_TYPE.Category &&
          channel.parentCategoryId === null
      )
      .sort(
        (first, second) =>
          first.position -
          second.position
      );

  return (
    <main className="ml-[88px] flex min-h-screen bg-background text-white">
      <aside className="flex w-60 flex-col bg-semibackground">
        <header className="border-b border-black/30 px-4 py-4 shadow">
 <div className="relative flex items-start justify-between gap-3">
    <div className="min-w-0">
      <h1 className="truncate font-semibold">
        {server.name}
      </h1>

      {server.description && (
        <p className="mt-1 truncate text-xs text-gray-400">
          {server.description}
        </p>
      )}
    </div>

<div className="relative flex-none">
  <button
    type="button"
    aria-label="Open server menu"
    aria-expanded={isServerMenuOpen}
    onClick={() =>
      setIsServerMenuOpen(
        current => !current
      )
    }
    className="flex h-8 w-8 items-center justify-center rounded-md text-xl text-gray-400 hover:bg-white/10 hover:text-white"
  >
    ⌄
  </button>

  {isServerMenuOpen && (
    <div className="absolute right-0 top-9 z-50 w-56 rounded-md bg-[#111214] p-2 shadow-xl">
      {canManageChannels && (
        <>
          <button
            type="button"
            onClick={() => {
              setNewChannelName("");
              setNewChannelType(
                CHANNEL_TYPE.Text
              );
              setNewChannelParentCategoryId(
                null
              );
              setCreateChannelError(null);

              setIsCreateChannelOpen(true);
              setIsServerMenuOpen(false);
            }}
            className="flex w-full items-center rounded px-2 py-2 text-left text-sm text-gray-200 hover:bg-primary hover:text-white"
          >
            Create Channel
          </button>

          <button
            type="button"
            onClick={() => {
              setNewCategoryName("");
              setCreateCategoryError(null);

              setIsCreateCategoryOpen(true);
              setIsServerMenuOpen(false);
            }}
            className="flex w-full items-center rounded px-2 py-2 text-left text-sm text-gray-200 hover:bg-primary hover:text-white"
          >
            Create Category
          </button>
        </>
      )}

      {isOwner && (
        <>
          <div className="my-2 border-t border-white/10" />

          <button
            type="button"
            onClick={() => {
              setIsSettingsOpen(true);
              setIsServerMenuOpen(false);
            }}
            className="flex w-full items-center gap-2 rounded px-2 py-2 text-left text-sm text-gray-200 hover:bg-primary hover:text-white"
          >
            <BsGearFill fontSize={14} />
            Server Settings
          </button>
        </>
      )}

      {!isOwner && (
        <>
          <div className="my-2 border-t border-white/10" />

          <button
            type="button"
            disabled={isLeaving}
            onClick={() => {
              setIsServerMenuOpen(false);
              void handleLeaveServer();
            }}
            className="flex w-full items-center rounded px-2 py-2 text-left text-sm text-red-400 hover:bg-red-500 hover:text-white disabled:opacity-50"
          >
            {isLeaving
              ? "Leaving..."
              : "Leave Server"}
          </button>
        </>
      )}
    </div>
  )}
</div>


  </div>
</header>

        <div className="flex-1 overflow-y-auto px-2 py-3">
        {canManageChannels && (
  <div className="mb-2 flex items-center justify-between px-2">
    <span className="text-xs font-bold uppercase text-gray-400">
      Channels
    </span>

    <button
      type="button"
      aria-label="Create channel"
      title="Create Channel"
      onClick={() => {
        setNewChannelName("");
        setNewChannelType(
          CHANNEL_TYPE.Text
        );
        setNewChannelParentCategoryId(
          null
        );
        setCreateChannelError(null);
        setIsCreateChannelOpen(true);
      }}
      className="flex h-6 w-6 items-center justify-center rounded text-xl text-gray-400 hover:bg-white/10 hover:text-white"
    >
      +
    </button>
  </div>
)}
          {uncategorizedChannels.map(
            channel => (
             <ChannelItem
  key={channel.id}
  channel={channel}
  serverId={server.id}
  canManageChannels={canManageChannels}
  onManageChannel={setChannelToManage}
/>
            )
          )}

          {categories.map(category => {
            const childChannels =
              server.channels
                .filter(
                  channel =>
                    channel.parentCategoryId ===
                    category.id
                )
                .sort(
                  (first, second) =>
                    first.position -
                    second.position
                );

            return (
              <section
                key={category.id}
                className="mt-4"
              >
               <div className="mb-1 flex items-center justify-between px-2">
  <h2 className="text-[11px] font-bold uppercase tracking-wide text-gray-400">
    {category.name}
  </h2>

  {canManageChannels && (
  <div className="flex items-center gap-1">
    <button
      type="button"
      aria-label={`Create channel in ${category.name}`}
      title="Create Channel"
      onClick={() => {
        setNewChannelName("");
        setNewChannelType(
          CHANNEL_TYPE.Text
        );
        setNewChannelParentCategoryId(
          category.id
        );
        setCreateChannelError(null);
        setIsCreateChannelOpen(true);
      }}
      className="flex h-5 w-5 items-center justify-center rounded text-lg text-gray-400 hover:text-white"
    >
      +
    </button>

    <button
      type="button"
      aria-label={`Edit ${category.name}`}
      title="Edit Category"
      onClick={() => {
        setChannelManageError(null);
        setIsChannelSaved(false);
        setChannelToManage(category);
      }}
      className="flex h-5 w-5 items-center justify-center rounded text-gray-400 hover:text-white"
    >
      <BsGearFill fontSize={11} />
    </button>
  </div>
)}
</div>

               {childChannels.map(channel => (
  <ChannelItem
    key={channel.id}
    channel={channel}
    serverId={server.id}
    canManageChannels={canManageChannels}
    onManageChannel={setChannelToManage}
  />
))}
              </section>
            );
          })}
        </div>

      <VoiceStatusFooter />
      </aside>

      <section className="flex flex-1 items-center justify-center">
        <div className="text-center">
          <h2 className="text-xl font-semibold">
            {server.name}
          </h2>

          <p className="mt-2 text-sm text-gray-400">
            Text kanal seçərək mesajları görə
            və ya voice kanala qoşula bilərsiniz.
          </p>
        </div>
      </section>

      {accessToken && (
       <ServerMemberList
  serverId={server.id}
  accessToken={accessToken}
onMemberRemoved={() => {
  window.dispatchEvent(
    new CustomEvent(
      "server-members:changed",
      {
        detail: {
          serverId: server.id,
        },
      }
    )
  );
}}
/>
      )}

      {isCreateCategoryOpen && (
  <div
    className="fixed inset-0 z-[130] flex items-center justify-center bg-black/70 px-4"
    onMouseDown={() => {
      setIsCreateCategoryOpen(false);
      setCreateCategoryError(null);
    }}
  >
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Create Category"
      onMouseDown={event =>
        event.stopPropagation()
      }
      className="relative w-full max-w-md rounded-xl bg-[#313338] p-6 text-white shadow-2xl"
    >
      <button
        type="button"
        aria-label="Close"
        onClick={() => {
          setIsCreateCategoryOpen(false);
          setCreateCategoryError(null);
        }}
        className="absolute right-4 top-3 text-2xl text-gray-400 hover:text-white"
      >
        ×
      </button>

      <h2 className="text-xl font-bold">
        Create Category
      </h2>

      <p className="mt-1 text-sm text-gray-400">
        Organize your channels into a new category.
      </p>

      <label className="mt-6 block text-xs font-bold uppercase text-gray-300">
        Category Name
      </label>

      <input
        value={newCategoryName}
        onChange={event => {
          setNewCategoryName(
            event.target.value
          );
          setCreateCategoryError(null);
        }}
        placeholder="New Category"
        maxLength={100}
        autoFocus
        className="mt-2 w-full rounded-md bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-primary"
      />

      {createCategoryError && (
        <p className="mt-4 rounded-md bg-red-500/10 px-3 py-2 text-sm text-red-400">
          {createCategoryError}
        </p>
      )}

      <div className="mt-6 flex justify-end gap-3">
        <button
          type="button"
          onClick={() => {
            setIsCreateCategoryOpen(false);
            setCreateCategoryError(null);
          }}
          disabled={isCreatingCategory}
          className="px-4 py-2.5 text-sm font-semibold text-gray-300 hover:underline disabled:opacity-50"
        >
          Cancel
        </button>

        <button
          type="button"
          onClick={() =>
            void handleCreateCategory()
          }
          disabled={
            isCreatingCategory ||
            !newCategoryName.trim()
          }
          className="rounded-md bg-primary px-5 py-2.5 text-sm font-semibold hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isCreatingCategory
            ? "Creating..."
            : "Create Category"}
        </button>
      </div>
    </div>
  </div>
)}
      {isCreateChannelOpen && (
  <div
    className="fixed inset-0 z-[130] flex items-center justify-center bg-black/70 px-4"
    onMouseDown={() =>
      setIsCreateChannelOpen(false)
    }
  >
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Create Channel"
      onMouseDown={event =>
        event.stopPropagation()
      }
      className="relative w-full max-w-md rounded-xl bg-[#313338] p-6 text-white shadow-2xl"
    >
      <button
        type="button"
        aria-label="Close"
        onClick={() =>
          setIsCreateChannelOpen(false)
        }
        className="absolute right-4 top-3 text-2xl text-gray-400 hover:text-white"
      >
        ×
      </button>

      <h2 className="text-xl font-bold">
        Create Channel
      </h2>

      <p className="mt-1 text-sm text-gray-400">
        Create a new channel or category.
      </p>

      <label className="mt-6 block text-xs font-bold uppercase text-gray-300">
        Channel Type
      </label>

      <select
        value={newChannelType}
        onChange={event => {
          const type = Number(
            event.target.value
          );

          setNewChannelType(type);

          if (
            type ===
            CHANNEL_TYPE.Category
          ) {
            setNewChannelParentCategoryId(
              null
            );
          }
        }}
        className="mt-2 w-full rounded-md bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-primary"
      >
        <option value={CHANNEL_TYPE.Text}>
          Text Channel
        </option>

        <option value={CHANNEL_TYPE.Voice}>
          Voice Channel
        </option>

      
      </select>

      <label className="mt-5 block text-xs font-bold uppercase text-gray-300">
        {newChannelType ===
        CHANNEL_TYPE.Category
          ? "Category Name"
          : "Channel Name"}
      </label>

      <input
        value={newChannelName}
        onChange={event =>
          setNewChannelName(
            event.target.value
          )
        }
        placeholder={
          newChannelType ===
          CHANNEL_TYPE.Category
            ? "GIRLS"
            : "makeup"
        }
        maxLength={100}
        className="mt-2 w-full rounded-md bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-primary"
      />



      {createChannelError && (
        <p className="mt-4 rounded-md bg-red-500/10 px-3 py-2 text-sm text-red-400">
          {createChannelError}
        </p>
      )}

      <div className="mt-6 flex justify-end gap-3">
        <button
          type="button"
          onClick={() =>
            setIsCreateChannelOpen(false)
          }
          disabled={isCreatingChannel}
          className="px-4 py-2.5 text-sm font-semibold text-gray-300 hover:underline disabled:opacity-50"
        >
          Cancel
        </button>

        <button
          type="button"
          onClick={() =>
            void handleCreateChannel()
          }
          disabled={
            isCreatingChannel ||
            !newChannelName.trim()
          }
          className="rounded-md bg-primary px-5 py-2.5 text-sm font-semibold hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isCreatingChannel
            ? "Creating..."
            : newChannelType ===
                CHANNEL_TYPE.Category
              ? "Create Category"
              : "Create Channel"}
        </button>
      </div>
    </div>
  </div>
)}

{isDeleteChannelConfirmOpen &&
  channelToManage && (
    <div
      className="fixed inset-0 z-[140] flex items-center justify-center bg-black/75 px-4"
      onMouseDown={() =>
        setIsDeleteChannelConfirmOpen(false)
      }
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Delete Channel"
        onMouseDown={event =>
          event.stopPropagation()
        }
        className="w-full max-w-md overflow-hidden rounded-xl bg-[#313338] text-white shadow-2xl"
      >
        <div className="p-6">
        <h2 className="text-xl font-bold">
  {channelToManage.type === CHANNEL_TYPE.Category
    ? "Delete Category"
    : "Delete Channel"}
</h2>

       <p className="mt-3 text-sm leading-6 text-gray-300">
  Are you sure you want to delete{" "}
  <span className="font-semibold text-white">
    {channelToManage.type === CHANNEL_TYPE.Category
      ? channelToManage.name
      : `#${channelToManage.name}`}
  </span>
  ?
</p>

          <p className="mt-2 text-sm text-gray-400">
            This action cannot be undone.
          </p>
        </div>

        <div className="flex justify-end gap-3 bg-[#2b2d31] px-6 py-4">
          <button
            type="button"
            disabled={isDeletingChannel}
            onClick={() =>
              setIsDeleteChannelConfirmOpen(false)
            }
            className="px-4 py-2.5 text-sm font-semibold text-white hover:underline disabled:opacity-50"
          >
            Cancel
          </button>

          <button
            type="button"
            disabled={isDeletingChannel}
            onClick={() =>
              void handleDeleteChannel()
            }
            className="rounded-md bg-red-500 px-4 py-2.5 text-sm font-semibold text-white hover:bg-red-600 disabled:cursor-wait disabled:opacity-50"
          >
            {isDeletingChannel
  ? "Deleting..."
  : channelToManage.type === CHANNEL_TYPE.Category
    ? "Delete Category"
    : "Delete Channel"}
          </button>
        </div>
      </div>
    </div>
  )}
{channelToManage && (
  <div
    className="fixed inset-0 z-[130] flex items-center justify-center bg-black/70 px-4"
    onMouseDown={() =>
      setChannelToManage(null)
    }
  >
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Channel Settings"
      onMouseDown={event =>
        event.stopPropagation()
      }
      className="relative w-full max-w-lg rounded-xl bg-[#313338] p-6 shadow-2xl"
    >
      <button
        type="button"
        aria-label="Close"
        onClick={() =>
          setChannelToManage(null)
        }
        className="absolute right-4 top-3 text-2xl text-gray-400 hover:text-white"
      >
        ×
      </button>
<h2 className="text-xl font-bold">
  {channelToManage.type ===
  CHANNEL_TYPE.Category
    ? "Category Settings"
    : "Channel Settings"}
</h2>

<p className="mt-1 text-sm text-gray-400">
  {channelToManage.type ===
  CHANNEL_TYPE.Category
    ? channelToManage.name
    : `#${channelToManage.name}`}
</p>

<div className="mt-6">
 <label className="block text-xs font-bold uppercase text-gray-300">
  {channelToManage.type === CHANNEL_TYPE.Category
    ? "Category Name"
    : "Channel Name"}
</label>

  <input
    value={channelToManage.name}
    onChange={event => {
  setIsChannelSaved(false);

  setChannelToManage({
    ...channelToManage,
    name: event.target.value,
  });
}
    }
    maxLength={100}
    className="mt-2 w-full rounded-md bg-[#1e1f22] px-3 py-3 text-sm text-white outline-none focus:ring-2 focus:ring-primary"
  />

  {channelToManage.type ===
    CHANNEL_TYPE.Text && (
    <>
      <label className="mt-5 block text-xs font-bold uppercase text-gray-300">
        Channel Topic
      </label>

      <textarea
        value={
          channelToManage.topic ?? ""
        }
       onChange={event => {
  setIsChannelSaved(false);

  setChannelToManage({
    ...channelToManage,
    topic: event.target.value,
  });
}}
        rows={3}
        maxLength={1024}
        className="mt-2 w-full resize-none rounded-md bg-[#1e1f22] px-3 py-3 text-sm text-white outline-none focus:ring-2 focus:ring-primary"
      />
    </>
  )}

{channelToManage.type !== CHANNEL_TYPE.Category && (
  <label className="mt-5 flex cursor-pointer items-center gap-3 text-sm text-gray-200">
    <input
      type="checkbox"
      checked={channelToManage.isPrivate}
      onChange={event => {
        setIsChannelSaved(false);

        setChannelToManage({
          ...channelToManage,
          isPrivate: event.target.checked,
        });
      }}
      className="h-4 w-4 accent-primary"
    />

    Private Channel
  </label>
)}
{channelManageError && (
  <p className="mt-4 rounded-md bg-red-500/10 px-3 py-2 text-sm text-red-400">
    {channelManageError}
  </p>
)}
  <div className="mt-6 flex items-center justify-between border-t border-white/10 pt-4">
    <button
  type="button"
  onClick={() =>
    void handleDeleteChannel()
  }
  disabled={
    isDeletingChannel ||
    isSavingChannel
  }
  className="rounded-md px-4 py-2.5 text-sm font-semibold text-red-400 hover:bg-red-500/10 disabled:cursor-not-allowed disabled:opacity-50"
>
{isDeletingChannel
  ? "Deleting..."
  : channelToManage.type === CHANNEL_TYPE.Category
    ? "Delete Category"
    : "Delete Channel"}
</button>

    <button
  type="button"
  onClick={() =>
    void handleSaveChannel()
  }
  disabled={
    isSavingChannel ||
    !channelToManage.name.trim()
  }
  className="rounded-md bg-primary px-5 py-2.5 text-sm font-semibold hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-50"
>
{isSavingChannel
  ? "Saving..."
  : isChannelSaved
    ? "Saved ✓"
    : "Save Changes"}
</button>
  </div>
</div>
    </div>
  </div>
)}
      {isOwner && accessToken && (
  <ServerSettingsModal
    open={isSettingsOpen}
    server={server}
    accessToken={accessToken}
    onClose={() =>
      setIsSettingsOpen(false)
    }
    onServerUpdated={
      handleServerUpdated
    }
  />
)}
    </main>
  );
}