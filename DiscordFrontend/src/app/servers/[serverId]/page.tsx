"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  useParams,
  useRouter,
} from "next/navigation";

import {
  ChannelResponse,
  getServerById,
  leaveServer,
  ServerDetailsResponse,
  ServerResponse,
} from "@/lib/serverApi";

import { useAuthStore } from "@/state/auth";

import {
  useVoice,
} from "@/components/voice/voice-provider";

import { BsGearFill } from "react-icons/bs";

import ServerSettingsModal from "@/components/islets/server-settings-modal";
import ServerMemberList from "@/components/islets/server-member-list";
import { useCurrentUserStore } from "@/state/user";

const CHANNEL_TYPE = {
  Text: 0,
  Voice: 1,
  Category: 2,
} as const;

function ChannelItem({
  channel,
  serverId,
}: {
  channel: ChannelResponse;
  serverId: number;
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

    

  const className =
    "flex w-full items-center gap-2 rounded px-2 py-1.5 text-left text-sm text-gray-400 hover:bg-white/5 hover:text-gray-200";

  if (channel.type === CHANNEL_TYPE.Text) {
    return (
      <Link
        href={`/servers/${serverId}/channels/${channel.id}`}
        className={className}
      >
        <span className="w-5 text-center text-lg">
          #
        </span>

        <span className="truncate">
          {channel.name}
        </span>
      </Link>
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

        const response =
          await getServerById(
            serverId,
            accessToken
          );

        if (!isCancelled) {
          setServer(response);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : "Server yüklənə bilmədi."
          );
        }
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
const isOwner =
  server.ownerId.toString() ===
  currentUser?.id;


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
  <div className="flex items-start justify-between gap-3">
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

    {isOwner && (
      <button
        type="button"
        aria-label="Open server settings"
        title="Server Settings"
        onClick={() =>
          setIsSettingsOpen(true)
        }
        className="flex h-8 w-8 flex-none items-center justify-center rounded-md text-gray-400 transition-colors hover:bg-white/10 hover:text-white"
      >
        <BsGearFill fontSize={16} />
      </button>
    )}

    {!isOwner && (
  <button
    type="button"
    disabled={isLeaving}
    onClick={
      handleLeaveServer
    }
    className="flex-none rounded-md px-2 py-1.5 text-xs font-semibold text-red-400 transition-colors hover:bg-red-500/10 hover:text-red-300 disabled:cursor-wait disabled:opacity-50"
  >
    {isLeaving
      ? "Leaving..."
      : "Leave Server"}
  </button>
)}
  </div>
</header>

        <div className="flex-1 overflow-y-auto px-2 py-3">
          {uncategorizedChannels.map(
            channel => (
              <ChannelItem
                key={channel.id}
                channel={channel}
                serverId={server.id}
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
                <h2 className="mb-1 px-2 text-[11px] font-bold uppercase tracking-wide text-gray-400">
                  {category.name}
                </h2>

                {childChannels.length > 0 ? (
                  childChannels.map(
                    channel => (
                      <ChannelItem
                        key={channel.id}
                        channel={channel}
                        serverId={server.id}
                      />
                    )
                  )
                ) : (
                  <p className="px-2 py-1 text-xs text-gray-500">
                    Kanal yoxdur
                  </p>
                )}
              </section>
            );
          })}
        </div>

        <footer className="border-t border-black/30 px-4 py-3 text-xs text-gray-400">
          👥 {server.memberCount} üzv
        </footer>
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