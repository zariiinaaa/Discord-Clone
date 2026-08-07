"use client";

import {
  useEffect,
  useState,
} from "react";

import Image from "next/image";

import {
  banServerMember,
  getServerMembers,
  kickServerMember,
} from "@/lib/serverApi";

import type {
  ServerMemberResponse,
} from "@/lib/serverApi";
import {
  assignServerRole,
  getServerRoles,
  removeServerRole,
} from "@/lib/serverRoleApi";

import type {
  ServerRoleResponse,
} from "@/lib/serverRoleApi";


import {
  ServerPermission,
} from "@/lib/serverPermissions";
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
function getHighestRole(
  member: ServerMemberResponse
): ServerRoleResponse | null {
  return [...member.roles]
    .filter(role => !role.isDefault)
    .sort(
      (firstRole, secondRole) =>
        secondRole.position -
        firstRole.position
    )[0] ?? null;
}

function getHighestDisplayedRole(
  member: ServerMemberResponse
): ServerRoleResponse | null {
  return [...member.roles]
    .filter(
      role =>
        !role.isDefault &&
        role.isDisplayedSeparately
    )
    .sort(
      (firstRole, secondRole) =>
        secondRole.position -
        firstRole.position
    )[0] ?? null;
}
function MemberItem({
  member,
  canKick,
  canBan,
  isKicking,
  onKick,
  onOpenActionMenu,
}: {
  member: ServerMemberResponse;
  canKick: boolean;
  canBan: boolean;
  canManageRoles: boolean;
  isKicking: boolean;
  isManagingRoles: boolean;
  changingRoleId: number | null;
  availableRoles: ServerRoleResponse[];

  onKick: (
    member: ServerMemberResponse
  ) => void;
onOpenActionMenu: (
  member: ServerMemberResponse,
  x: number,
  y: number
) => void;
  onToggleRoles: (
    memberUserId: number
  ) => void;

  onToggleRole: (
    member: ServerMemberResponse,
    role: ServerRoleResponse
  ) => void;
}) {
  const name = getMemberName(member);

  const avatarSource =
    getAvatarSource(member.avatarUrl);

  const isOffline =
    member.status === 0;

  const highestRole =
    getHighestRole(member);

  return (
    <div
onContextMenu={event => {
  if (
    (!canKick && !canBan) ||
    isKicking
  ) {
    return;
  }

  event.preventDefault();

  onOpenActionMenu(
    member,
    event.clientX,
    event.clientY
  );
}}
    
      className={`flex items-center gap-3 rounded-md px-2 py-1.5 hover:bg-white/5 ${
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
          <p
            className="truncate text-sm font-medium"
            style={{
              color:
                highestRole?.colorHex ??
                "#D1D5DB",
            }}
          >
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
const [roles, setRoles] =
  useState<ServerRoleResponse[]>([]);

const [
  managingMemberUserId,
  setManagingMemberUserId,
] = useState<number | null>(null);

const [
  changingRoleId,
  setChangingRoleId,
] = useState<number | null>(null);
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
  memberToBan,
  setMemberToBan,
] = useState<ServerMemberResponse | null>(null);
const [
  memberActionMenu,
  setMemberActionMenu,
] = useState<{
  member: ServerMemberResponse;
  x: number;
  y: number;
} | null>(null);
const [
  banReason,
  setBanReason,
] = useState("");

const [
  isBanningMember,
  setIsBanningMember,
] = useState(false);
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

        const [
  membersResponse,
  rolesResponse,
] = await Promise.all([
  getServerMembers(
    serverId,
    accessToken
  ),
  getServerRoles(
    serverId,
    accessToken
  ),
]);

if (!isCancelled) {
  setMembers(membersResponse);

  setRoles(
    [...rolesResponse].sort(
      (firstRole, secondRole) =>
        secondRole.position -
        firstRole.position
    )
  );
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

    const currentUserMember = members.find(
  member => member.userId === currentUserId
);

const canManageRoles =
  isCurrentUserOwner ||
  currentUserMember?.roles.some(role =>
    role.permissions.includes(
      ServerPermission.Administrator
    ) ||
    role.permissions.includes(
      ServerPermission.ManageRoles
    )
  ) === true;
const canKickMembers =
  isCurrentUserOwner ||
  currentUserMember?.roles.some(role =>
    role.permissions.includes(
      ServerPermission.Administrator
    ) ||
    role.permissions.includes(
      ServerPermission.KickMembers
    )
  ) === true;
  const canBanMembers =
  isCurrentUserOwner ||
  currentUserMember?.roles.some(role =>
    role.permissions.includes(
      ServerPermission.Administrator
    ) ||
    role.permissions.includes(
      ServerPermission.BanMembers
    )
  ) === true;
  const handleKickMember = async (
    member: ServerMemberResponse
  ) => {
    if (
       !canKickMembers ||
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
const handleBanMember = async () => {
  if (
    !memberToBan ||
    !canBanMembers ||
    memberToBan.isOwner ||
    memberToBan.userId === currentUserId ||
    isBanningMember
  ) {
    return;
  }

  try {
    setIsBanningMember(true);
    setActionError(null);

    await banServerMember(
      serverId,
      memberToBan.userId,
      banReason.trim() || null,
      accessToken
    );

    setMembers(currentMembers =>
      currentMembers.filter(
        member =>
          member.userId !==
          memberToBan.userId
      )
    );

    setMemberToBan(null);
    setBanReason("");

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
  } catch (banError) {
    setActionError(
      banError instanceof Error
        ? banError.message
        : "Member could not be banned."
    );
  } finally {
    setIsBanningMember(false);
  }
};
const handleToggleMemberRole = async (
  member: ServerMemberResponse,
  role: ServerRoleResponse
) => {
  if (
    role.isDefault ||
    changingRoleId !== null
  ) {
    return;
  }

  const isRoleAssigned =
    member.roles.some(
      memberRole =>
        memberRole.id === role.id
    );

  try {
    setChangingRoleId(role.id);
    setActionError(null);

    if (isRoleAssigned) {
      await removeServerRole(
        serverId,
        role.id,
        member.userId,
        accessToken
      );
    } else {
      await assignServerRole(
        serverId,
        role.id,
        member.userId,
        accessToken
      );
    }

    const refreshedMembers =
      await getServerMembers(
        serverId,
        accessToken
      );

    setMembers(refreshedMembers);
    setManagingMemberUserId(null);
  } catch (roleError) {
    setActionError(
      roleError instanceof Error
        ? roleError.message
        : "Member role could not be changed."
    );
  } finally {
    setChangingRoleId(null);
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

 


const displayedRoleGroups = roles
  .filter(
    role =>
      !role.isDefault &&
      role.isDisplayedSeparately
  )
  .sort(
    (firstRole, secondRole) =>
      secondRole.position -
      firstRole.position
  )
  .map(role => ({
    role,

    members: sortedMembers.filter(
      member =>
        getHighestDisplayedRole(member)
          ?.id === role.id
    ),
  }))
  .filter(
    group =>
      group.members.length > 0
  );

const groupedRoleUserIds = new Set(
  displayedRoleGroups.flatMap(
    group =>
      group.members.map(
        member => member.userId
      )
  )
);

const onlineMembers =
  sortedMembers.filter(
    member =>
      member.status !== 0 &&
      !groupedRoleUserIds.has(
        member.userId
      )
  );

const offlineMembers =
  sortedMembers.filter(
    member =>
      member.status === 0 &&
      !groupedRoleUserIds.has(
        member.userId
      )
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
  displayedRoleGroups.map(group => (
    <section
      key={group.role.id}
      className="mb-5"
    >
      <h2
        className="mb-1 px-2 text-[11px] font-semibold uppercase"
        style={{
          color:
            group.role.colorHex ??
            "#949BA4",
        }}
      >
        {group.role.name} —{" "}
        {group.members.length}
      </h2>

      {group.members.map(member => (
        <MemberItem
          key={member.id}
          member={member}
         canKick={
  canKickMembers &&
  !member.isOwner &&
  member.userId !== currentUserId
}

canBan={
  canBanMembers &&
  !member.isOwner &&
  member.userId !== currentUserId
}

onOpenActionMenu={(
  selectedMember,
  x,
  y
) => {
  setMemberActionMenu({
    member: selectedMember,
    x,
    y,
  });
}}
          canManageRoles={
            canManageRoles &&
            (
              isCurrentUserOwner ||
              (
                !member.isOwner &&
                member.userId !==
                  currentUserId
              )
            )
          }
          isKicking={
            kickingUserId ===
            member.userId
          }
          isManagingRoles={
            managingMemberUserId ===
            member.userId
          }
          changingRoleId={
            changingRoleId
          }
          availableRoles={roles}
          onKick={handleKickMember}
          onToggleRoles={memberUserId =>
            setManagingMemberUserId(
              currentManagingUserId =>
                currentManagingUserId ===
                memberUserId
                  ? null
                  : memberUserId
            )
          }
          onToggleRole={
            handleToggleMemberRole
          }
        />
      ))}
    </section>
  ))}
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
  canKickMembers &&
  !member.isOwner &&
  member.userId !== currentUserId
}
canBan={
  canBanMembers &&
  !member.isOwner &&
  member.userId !== currentUserId
}

onOpenActionMenu={(
  selectedMember,
  x,
  y
) => {
  setMemberActionMenu({
    member: selectedMember,
    x,
    y,
  });
}}
  canManageRoles={
    canManageRoles &&
    (
      isCurrentUserOwner ||
      (
        !member.isOwner &&
        member.userId !== currentUserId
      )
    )
  }
  isKicking={
    kickingUserId === member.userId
  }
  isManagingRoles={
    managingMemberUserId === member.userId
  }
  changingRoleId={changingRoleId}
  availableRoles={roles}
  onKick={handleKickMember}
  onToggleRoles={memberUserId =>
    setManagingMemberUserId(
      currentUserId =>
        currentUserId === memberUserId
          ? null
          : memberUserId
    )
  }
  onToggleRole={handleToggleMemberRole}
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
  canKickMembers &&
  !member.isOwner &&
  member.userId !== currentUserId
}

canBan={
  canBanMembers &&
  !member.isOwner &&
  member.userId !== currentUserId
}

onOpenActionMenu={(
  selectedMember,
  x,
  y
) => {
  setMemberActionMenu({
    member: selectedMember,
    x,
    y,
  });
}}
  canManageRoles={
    canManageRoles &&
    (
      isCurrentUserOwner ||
      (
        !member.isOwner &&
        member.userId !== currentUserId
      )
    )
  }
  isKicking={
    kickingUserId === member.userId
  }
  isManagingRoles={
    managingMemberUserId === member.userId
  }
  changingRoleId={changingRoleId}
  availableRoles={roles}
  onKick={handleKickMember}
  onToggleRoles={memberUserId =>
    setManagingMemberUserId(
      currentUserId =>
        currentUserId === memberUserId
          ? null
          : memberUserId
    )
  }
  onToggleRole={handleToggleMemberRole}
/>
              )
            )}
          </section>
        )}
{memberToBan && (
  <div
    className="fixed inset-0 z-[210] flex items-center justify-center bg-black/75 px-4"
    onMouseDown={() => {
      if (isBanningMember) {
        return;
      }

      setMemberToBan(null);
      setBanReason("");
    }}
  >
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Ban Member"
      onMouseDown={event =>
        event.stopPropagation()
      }
      className="w-full max-w-md overflow-hidden rounded-xl bg-[#313338] text-white shadow-2xl"
    >
      <div className="p-6">
        <h2 className="text-xl font-bold">
          Ban Member
        </h2>

        <p className="mt-2 text-sm text-gray-300">
          Are you sure you want to ban{" "}
          <span className="font-semibold text-white">
            {getMemberName(memberToBan)}
          </span>
          ?
        </p>

        <label className="mt-5 block text-xs font-bold uppercase text-gray-300">
          Reason
        </label>

        <textarea
          value={banReason}
          onChange={event =>
            setBanReason(
              event.target.value
            )
          }
          maxLength={500}
          rows={3}
          placeholder="Optional reason"
          className="mt-2 w-full resize-none rounded-md bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-primary"
        />

        {actionError && (
          <p className="mt-4 rounded-md bg-red-500/10 px-3 py-2 text-sm text-red-400">
            {actionError}
          </p>
        )}
      </div>

      <div className="flex justify-end gap-3 bg-[#2b2d31] px-6 py-4">
        <button
          type="button"
          disabled={isBanningMember}
          onClick={() => {
            setMemberToBan(null);
            setBanReason("");
          }}
          className="px-4 py-2.5 text-sm font-semibold text-white hover:underline disabled:opacity-50"
        >
          Cancel
        </button>

        <button
          type="button"
          disabled={isBanningMember}
          onClick={() =>
            void handleBanMember()
          }
          className="rounded-md bg-red-500 px-4 py-2.5 text-sm font-semibold text-white hover:bg-red-600 disabled:cursor-wait disabled:opacity-50"
        >
          {isBanningMember
            ? "Banning..."
            : "Ban Member"}
        </button>
      </div>
    </div>
  </div>
)}
{memberActionMenu && (
  <>
    <button
      type="button"
      aria-label="Close member menu"
      onClick={() =>
        setMemberActionMenu(null)
      }
      className="fixed inset-0 z-[190] cursor-default"
    />

    <div
      className="fixed z-[200] w-56 rounded-md bg-[#111214] p-1.5 shadow-2xl"
    style={{
  left: Math.min(
    memberActionMenu.x,
    window.innerWidth - 240
  ),
  top: Math.min(
    memberActionMenu.y,
    window.innerHeight - 150
  ),
}}
    >
      <div className="px-2.5 py-2">
        <p className="truncate text-xs font-bold text-gray-300">
          {getMemberName(
            memberActionMenu.member
          )}
        </p>

        <p className="mt-0.5 text-[11px] text-gray-500">
          Member Actions
        </p>
      </div>

      <div className="my-1 border-t border-white/10" />

      {canKickMembers &&
        !memberActionMenu.member.isOwner &&
        memberActionMenu.member.userId !==
          currentUserId && (
          <button
            type="button"
            onClick={() => {
              const member =
                memberActionMenu.member;

              setMemberActionMenu(null);

              void handleKickMember(
                member
              );
            }}
            className="flex w-full items-center rounded px-2.5 py-2 text-left text-sm font-medium text-red-400 hover:bg-red-500 hover:text-white"
          >
            Kick Member
          </button>
        )}

      {canBanMembers &&
        !memberActionMenu.member.isOwner &&
        memberActionMenu.member.userId !==
          currentUserId && (
          <button
            type="button"
            onClick={() => {
              setMemberToBan(
                memberActionMenu.member
              );

              setBanReason("");
              setMemberActionMenu(null);
            }}
            className="flex w-full items-center rounded px-2.5 py-2 text-left text-sm font-medium text-red-400 hover:bg-red-500 hover:text-white"
          >
            Ban Member
          </button>
        )}
    </div>
  </>
)}
    </aside>
  );
}