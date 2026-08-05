"use client";

import {
  useEffect,
  useState,
} from "react";

import Image from "next/image";

import {
  getServerMembers,
  kickServerMember,
} from "@/lib/serverApi";

import type {
  ServerMemberResponse,
} from "@/lib/serverApi";


import {
  useCurrentUserStore,
} from "@/state/user";

import { API_URL } from "@/lib/api";

interface ServerMemberListProps {
  serverId: number;
  accessToken: string;
  onMemberRemoved: () => void;
}

function getAvatarSource(
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
    avatarUrl.startsWith("/")
      ? ""
      : "/"
  }${avatarUrl}`;
}

function getMemberName(
  member: ServerMemberResponse
): string {
  return (
    member.nickname?.trim() ||
    member.displayName ||
    member.username
  );
}

function getStatusColor(
  status: number
): string {
  switch (status) {
    case 1:
      return "bg-green-500";

    case 2:
      return "bg-yellow-500";

    case 3:
      return "bg-red-500";

    default:
      return "bg-gray-500";
  }
}

function MemberItem({
  member,
  canKick,
  isKicking,
  onKick,
}: {
  member: ServerMemberResponse;
  canKick: boolean;
  isKicking: boolean;
  onKick: (
    member: ServerMemberResponse
  ) => void;
}) {
  const name = getMemberName(member);

  const avatarSource =
    getAvatarSource(member.avatarUrl);

  const isOffline =
    member.status === 0;

  return (
    <div
      className={`group flex items-center gap-3 rounded-md px-2 py-1.5 hover:bg-white/5 ${
        isOffline
          ? "opacity-50 hover:opacity-80"
          : ""
      }`}
    >
      <div className="relative h-8 w-8 flex-none">
        {avatarSource ? (
          <Image
            src={avatarSource}
            alt={name}
            width={32}
            height={32}
            unoptimized
            className="h-8 w-8 rounded-full object-cover"
          />
        ) : (
          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary text-sm font-semibold text-white">
            {name
              .charAt(0)
              .toUpperCase()}
          </div>
        )}

        <span
          className={`absolute -bottom-0.5 -right-0.5 h-3 w-3 rounded-full border-[3px] border-[#2b2d31] ${getStatusColor(
            member.status
          )}`}
        />
      </div>

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-1.5">
          <p className="truncate text-sm font-medium text-gray-300 group-hover:text-white">
            {name}
          </p>

          {member.isOwner && (
            <span
              title="Server Owner"
              aria-label="Server Owner"
              className="flex-none text-xs"
            >
              👑
            </span>
          )}
        </div>

        {member.nickname && (
          <p className="truncate text-[11px] text-gray-500">
            {member.displayName}
          </p>
        )}
      </div>

            {canKick && (
        <button
          type="button"
          disabled={isKicking}
          onClick={() => onKick(member)}
          className="hidden flex-none rounded px-2 py-1 text-xs font-medium text-red-400 hover:bg-red-500/10 hover:text-red-300 disabled:opacity-50 group-hover:block"
        >
          {isKicking
            ? "Kicking..."
            : "Kick"}
        </button>
      )}
    </div>
  );
}

export default function ServerMemberList({
  serverId,
  accessToken,
  onMemberRemoved,
}: ServerMemberListProps) {
  const [members, setMembers] =
    useState<ServerMemberResponse[]>(
      []
    );

      const currentUser =
    useCurrentUserStore(
      state => state.currentUser
    );

  const [isLoading, setIsLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(null);

      const [
    kickingUserId,
    setKickingUserId,
  ] = useState<number | null>(null);

  const [
    actionError,
    setActionError,
  ] = useState<string | null>(null);

  useEffect(() => {
    let isCancelled = false;

    const loadMembers = async () => {
      try {
        setIsLoading(true);
        setError(null);

        const response =
          await getServerMembers(
            serverId,
            accessToken
          );

        if (!isCancelled) {
          setMembers(response);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : "Members could not be loaded."
          );
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    };

    void loadMembers();

    return () => {
      isCancelled = true;
    };
  }, [
    serverId,
    accessToken,
  ]);

    useEffect(() => {
    const handlePresenceChanged = (
      event: Event
    ) => {
      const presenceEvent =
        event as CustomEvent<{
          userId: number;
          status: number;
        }>;

      const {
        userId,
        status,
      } = presenceEvent.detail;

      setMembers(currentMembers =>
        currentMembers.map(member =>
          member.userId === userId
            ? {
                ...member,
                status,
              }
            : member
        )
      );
    };

    window.addEventListener(
      "user-presence:changed",
      handlePresenceChanged
    );

    return () => {
      window.removeEventListener(
        "user-presence:changed",
        handlePresenceChanged
      );
    };
  }, []);
useEffect(() => {
  let isDisposed = false;

  const handleServerMembersChanged =
    async (event: Event) => {
      const membersEvent =
        event as CustomEvent<{
          serverId: number;
        }>;

      if (
        membersEvent.detail.serverId !==
        serverId
      ) {
        return;
      }

      try {
        const response =
          await getServerMembers(
            serverId,
            accessToken
          );

        if (!isDisposed) {
          setMembers(response);
          setActionError(null);
        }
      } catch (refreshError) {
        if (!isDisposed) {
          setActionError(
            refreshError instanceof Error
              ? refreshError.message
              : "Members could not be refreshed."
          );
        }
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
  serverId,
  accessToken,
]);

    const currentUserId =
    Number(currentUser?.id);

  const isCurrentUserOwner =
    members.some(
      member =>
        member.userId ===
          currentUserId &&
        member.isOwner
    );

  const handleKickMember = async (
    member: ServerMemberResponse
  ) => {
    if (
      !isCurrentUserOwner ||
      member.isOwner ||
      member.userId ===
        currentUserId ||
      kickingUserId !== null
    ) {
      return;
    }

    const shouldKick =
      window.confirm(
        `Remove ${getMemberName(
          member
        )} from this server?`
      );

    if (!shouldKick) {
      return;
    }

    try {
      setKickingUserId(
        member.userId
      );

      setActionError(null);

      await kickServerMember(
        serverId,
        member.userId,
        accessToken
      );

      setMembers(currentMembers =>
        currentMembers.filter(
          currentMember =>
            currentMember.userId !==
            member.userId
        )
      );
            onMemberRemoved();
    } catch (kickError) {
      setActionError(
        kickError instanceof Error
          ? kickError.message
          : "Member could not be removed."
      );
    } finally {
      setKickingUserId(null);
    }
  };


  const sortedMembers = [
    ...members,
  ].sort((first, second) => {
    if (
      first.isOwner !==
      second.isOwner
    ) {
      return first.isOwner ? -1 : 1;
    }

    return getMemberName(first)
      .localeCompare(
        getMemberName(second)
      );
  });

  const onlineMembers =
    sortedMembers.filter(
      member => member.status !== 0
    );

  const offlineMembers =
    sortedMembers.filter(
      member => member.status === 0
    );

  return (
    <aside className="hidden w-60 flex-none overflow-y-auto bg-[#2b2d31] px-2 py-5 lg:block">
              {actionError && (
        <p className="mb-3 rounded bg-red-500/10 px-2 py-2 text-xs text-red-400">
          {actionError}
        </p>
      )}
      {isLoading && (
        <p className="px-2 text-sm text-gray-500">
          Loading members...
        </p>
      )}

      {!isLoading && error && (
        <p className="px-2 text-sm text-red-400">
          {error}
        </p>
      )}

      {!isLoading &&
        !error &&
        members.length === 0 && (
          <p className="px-2 text-sm text-gray-500">
            No members found.
          </p>
        )}

      {!isLoading &&
        !error &&
        onlineMembers.length > 0 && (
          <section>
            <h2 className="mb-1 px-2 text-[11px] font-semibold uppercase text-gray-400">
              Online —{" "}
              {onlineMembers.length}
            </h2>

            {onlineMembers.map(
              member => (
                <MemberItem
  key={member.id}
  member={member}
  canKick={
    isCurrentUserOwner &&
    !member.isOwner &&
    member.userId !==
      currentUserId
  }
  isKicking={
    kickingUserId ===
    member.userId
  }
  onKick={
    handleKickMember
  }
/>
              )
            )}
          </section>
        )}

      {!isLoading &&
        !error &&
        offlineMembers.length > 0 && (
          <section className="mt-5">
            <h2 className="mb-1 px-2 text-[11px] font-semibold uppercase text-gray-400">
              Offline —{" "}
              {offlineMembers.length}
            </h2>

            {offlineMembers.map(
              member => (
                <MemberItem
  key={member.id}
  member={member}
  canKick={
    isCurrentUserOwner &&
    !member.isOwner &&
    member.userId !==
      currentUserId
  }
  isKicking={
    kickingUserId ===
    member.userId
  }
  onKick={
    handleKickMember
  }
/>
              )
            )}
          </section>
        )}
    </aside>
  );
}