"use client";

import {
  type ChangeEvent,
  type FormEvent,
  useEffect,
  useState,
} from "react";

import Image from "next/image";
import { createPortal } from "react-dom";

import { API_URL } from "@/lib/api";

import {
  createServerInvite,
  getServerInvites,
  getServerMembers,
  revokeServerInvite,
  updateServer,
  uploadServerIcon,
  getServerBans,
unbanServerMember,
} from "@/lib/serverApi";

import type {
  ServerDetailsResponse,
  ServerInviteResponse,
  ServerMemberResponse,
  ServerResponse,
  ServerBanResponse,
} from "@/lib/serverApi";


import {
  assignServerRole,
  createServerRole,
  deleteServerRole,
  getServerRoles,
  removeServerRole,
  updateServerRole,
} from "@/lib/serverRoleApi";

import type {
  ServerRoleResponse,
} from "@/lib/serverRoleApi";

import {
  SERVER_PERMISSION_OPTIONS,
} from "@/lib/serverPermissions";

interface ServerSettingsModalProps {
  open: boolean;
  server: ServerDetailsResponse;
  accessToken: string;
  onClose: () => void;
  onServerUpdated: (
    server: ServerResponse
  ) => void;
}

function getErrorMessage(
  error: unknown
): string {
  return error instanceof Error
    ? error.message
    : "Server settings could not be saved.";
}

function getServerIconSource(
  iconUrl: string | null
): string | null {
  if (!iconUrl) {
    return null;
  }

  if (
    iconUrl.startsWith("http://") ||
    iconUrl.startsWith("https://")
  ) {
    return iconUrl;
  }

  return `${API_URL}${
    iconUrl.startsWith("/") ? "" : "/"
  }${iconUrl}`;
}

export default function ServerSettingsModal({
  open,
  server,
  accessToken,
  onClose,
  onServerUpdated,
}: ServerSettingsModalProps) {

 const [activeSection, setActiveSection] =
  useState<
    "overview" | "roles" | "bans" | "invites"
  >("overview");
  const [name, setName] =
    useState(server.name);

  const [description, setDescription] =
    useState(
      server.description ?? ""
    );

  const [isPublic, setIsPublic] =
    useState(server.isPublic);

  const [
    selectedIcon,
    setSelectedIcon,
  ] = useState<File | null>(null);

  const [
    iconPreview,
    setIconPreview,
  ] = useState<string | null>(null);

  const [isSaving, setIsSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

    const [invites, setInvites] =
  useState<ServerInviteResponse[]>([]);

const [
  expirationHours,
  setExpirationHours,
] = useState("24");

const [maxUses, setMaxUses] =
  useState("10");

const [
  isLoadingInvites,
  setIsLoadingInvites,
] = useState(false);

const [
  isCreatingInvite,
  setIsCreatingInvite,
] = useState(false);

const [
  revokingInviteId,
  setRevokingInviteId,
] = useState<number | null>(null);

const [inviteError, setInviteError] =
  useState<string | null>(null);

const [copiedCode, setCopiedCode] =
  useState<string | null>(null);
const [roles, setRoles] =
  useState<ServerRoleResponse[]>([]);

const [selectedRoleId, setSelectedRoleId] =
  useState<number | null>(null);

const [roleName, setRoleName] =
  useState("");

const [roleColorHex, setRoleColorHex] =
  useState("");

const [
  rolePermissions,
  setRolePermissions,
] = useState<number[]>([]);

const [
  roleDisplayedSeparately,
  setRoleDisplayedSeparately,
] = useState(false);

const [
  roleMentionable,
  setRoleMentionable,
] = useState(false);

const [isLoadingRoles, setIsLoadingRoles] =
  useState(false);

const [isSavingRole, setIsSavingRole] =
  useState(false);

const [
  deletingRoleId,
  setDeletingRoleId,
] = useState<number | null>(null);

const [roleError, setRoleError] =
  useState<string | null>(null);

  const [isRoleSaved, setIsRoleSaved] =
  useState(false);

  const [roleMembers, setRoleMembers] =
  useState<ServerMemberResponse[]>([]);
const [bans, setBans] =
  useState<ServerBanResponse[]>([]);

const [
  isLoadingBans,
  setIsLoadingBans,
] = useState(false);

const [
  unbanningUserId,
  setUnbanningUserId,
] = useState<number | null>(null);

const [banError, setBanError] =
  useState<string | null>(null);
const [
  isLoadingRoleMembers,
  setIsLoadingRoleMembers,
] = useState(false);

const [
  changingRoleMemberId,
  setChangingRoleMemberId,
] = useState<number | null>(null);

const [
  roleMemberError,
  setRoleMemberError,
] = useState<string | null>(null);
  useEffect(() => {
    if (!open) {
      return;
    }
    setActiveSection("overview");

    setName(server.name);

    setDescription(
      server.description ?? ""
    );

    setIsPublic(server.isPublic);
    setSelectedIcon(null);
    setIconPreview(null);
    setError(null);
    setIsSaving(false);

    setInvites([]);
setExpirationHours("24");
setMaxUses("10");
setInviteError(null);
setCopiedCode(null);
setIsLoadingInvites(false);
setIsCreatingInvite(false);
setRevokingInviteId(null);
  }, [
    open,
    server.name,
    server.description,
    server.isPublic,
  ]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const handleKeyDown = (
      event: KeyboardEvent
    ) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    window.addEventListener(
      "keydown",
      handleKeyDown
    );

    return () => {
      window.removeEventListener(
        "keydown",
        handleKeyDown
      );
    };
  }, [open, onClose]);

  useEffect(() => {
    return () => {
      if (iconPreview) {
        URL.revokeObjectURL(
          iconPreview
        );
      }
    };
  }, [iconPreview]);

useEffect(() => {
  if (
    !open ||
    activeSection !== "invites"
  ) {
    return;
  }

  let isCancelled = false;

  const loadInvites = async () => {
    try {
      setIsLoadingInvites(true);
      setInviteError(null);

      const response =
        await getServerInvites(
          server.id,
          accessToken
        );

      if (!isCancelled) {
        setInvites(response);
      }
    } catch (loadError) {
      if (!isCancelled) {
        setInviteError(
          getErrorMessage(loadError)
        );
      }
    } finally {
      if (!isCancelled) {
        setIsLoadingInvites(false);
      }
    }
  };

  void loadInvites();

  return () => {
    isCancelled = true;
  };
}, [
  open,
  activeSection,
  server.id,
  accessToken,
]);

useEffect(() => {
  if (
    !open ||
    activeSection !== "roles"
  ) {
    return;
  }

  let isCancelled = false;

  const loadRoles = async () => {
    try {
      setIsLoadingRoles(true);
      setRoleError(null);

      const response =
        await getServerRoles(
          server.id,
          accessToken
        );

      if (isCancelled) {
        return;
      }

      const orderedRoles = [
        ...response,
      ].sort(
        (firstRole, secondRole) =>
          secondRole.position -
          firstRole.position
      );

      setRoles(orderedRoles);

      const firstRole =
        orderedRoles[0] ?? null;

      if (!firstRole) {
        setSelectedRoleId(null);
        setRoleName("");
        setRoleColorHex("");
        setRolePermissions([]);
        setRoleDisplayedSeparately(false);
        setRoleMentionable(false);
        return;
      }

      setSelectedRoleId(firstRole.id);
      setRoleName(firstRole.name);
      setRoleColorHex(
        firstRole.colorHex ?? ""
      );
      setRolePermissions(
        firstRole.permissions
      );
      setRoleDisplayedSeparately(
        firstRole.isDisplayedSeparately
      );
      setRoleMentionable(
        firstRole.isMentionable
      );
    } catch (loadError) {
      if (!isCancelled) {
        setRoleError(
          getErrorMessage(loadError)
        );
      }
    } finally {
      if (!isCancelled) {
        setIsLoadingRoles(false);
      }
    }
  };

  void loadRoles();

  return () => {
    isCancelled = true;
  };
}, [
  open,
  activeSection,
  server.id,
  accessToken,
]);

useEffect(() => {
  if (
    !open ||
    activeSection !== "roles"
  ) {
    return;
  }

  let isCancelled = false;

  const loadRoleMembers = async () => {
    try {
      setIsLoadingRoleMembers(true);
      setRoleMemberError(null);

      const response =
        await getServerMembers(
          server.id,
          accessToken
        );

      if (!isCancelled) {
        setRoleMembers(response);
      }
    } catch (loadError) {
      if (!isCancelled) {
        setRoleMemberError(
          getErrorMessage(loadError)
        );
      }
    } finally {
      if (!isCancelled) {
        setIsLoadingRoleMembers(false);
      }
    }
  };

  void loadRoleMembers();

  return () => {
    isCancelled = true;
  };
}, [
  open,
  activeSection,
  server.id,
  accessToken,
]);
useEffect(() => {
  if (
    !open ||
    activeSection !== "bans"
  ) {
    return;
  }

  let isCancelled = false;

  const loadBans = async () => {
    try {
      setIsLoadingBans(true);
      setBanError(null);

      const response =
        await getServerBans(
          server.id,
          accessToken
        );

      if (!isCancelled) {
        setBans(response);
      }
    } catch (loadError) {
      if (!isCancelled) {
        setBanError(
          getErrorMessage(loadError)
        );
      }
    } finally {
      if (!isCancelled) {
        setIsLoadingBans(false);
      }
    }
  };

  void loadBans();

  return () => {
    isCancelled = true;
  };
}, [
  open,
  activeSection,
  server.id,
  accessToken,
]);
  const handleIconChange = (
    event: ChangeEvent<HTMLInputElement>
  ) => {
    const file =
      event.target.files?.[0] ?? null;

    setError(null);

    if (!file) {
      setSelectedIcon(null);
      setIconPreview(null);
      return;
    }

    const allowedTypes = [
      "image/jpeg",
      "image/png",
      "image/webp",
    ];

    if (
      !allowedTypes.includes(file.type)
    ) {
      event.target.value = "";

      setError(
        "Only JPG, PNG and WEBP images are allowed."
      );

      return;
    }

    if (
      file.size >
      5 * 1024 * 1024
    ) {
      event.target.value = "";

      setError(
        "The server icon must not exceed 5 MB."
      );

      return;
    }

    setSelectedIcon(file);

    setIconPreview(
      URL.createObjectURL(file)
    );
  };


  const handleCreateInvite = async () => {
  if (isCreatingInvite) {
    return;
  }

  const parsedExpirationHours =
    expirationHours.trim()
      ? Number(expirationHours)
      : null;

  const parsedMaxUses =
    maxUses.trim()
      ? Number(maxUses)
      : null;

  if (
    parsedExpirationHours !== null &&
    (
      !Number.isInteger(
        parsedExpirationHours
      ) ||
      parsedExpirationHours < 1 ||
      parsedExpirationHours > 168
    )
  ) {
    setInviteError(
      "Expiration must be between 1 and 168 hours."
    );

    return;
  }

  if (
    parsedMaxUses !== null &&
    (
      !Number.isInteger(parsedMaxUses) ||
      parsedMaxUses < 1 ||
      parsedMaxUses > 100
    )
  ) {
    setInviteError(
      "Max uses must be between 1 and 100."
    );

    return;
  }
const inviteChannel =
  server.channels.find(
    channel => channel.type === 0
  );

if (!inviteChannel) {
  setInviteError(
    "Create a text channel before creating an invite."
  );
  return;
}
  try {
    setIsCreatingInvite(true);
    setInviteError(null);

    const createdInvite =
      await createServerInvite(
        server.id,
       {
  channelId: inviteChannel.id,
  expirationHours:
    parsedExpirationHours,
  maxUses: parsedMaxUses,
},
        accessToken
      );

    setInvites(currentInvites => [
      createdInvite,
      ...currentInvites,
    ]);
  } catch (createError) {
    setInviteError(
      getErrorMessage(createError)
    );
  } finally {
    setIsCreatingInvite(false);
  }
};

const handleCopyInvite = async (
  code: string
) => {
  try {
    await navigator.clipboard.writeText(
      code
    );

    setCopiedCode(code);

    window.setTimeout(() => {
      setCopiedCode(currentCode =>
        currentCode === code
          ? null
          : currentCode
      );
    }, 2000);
  } catch {
    setInviteError(
      "Invite code could not be copied."
    );
  }
};

const handleRevokeInvite = async (
  inviteId: number
) => {
  if (
    revokingInviteId !== null ||
    !window.confirm(
      "Revoke this invite?"
    )
  ) {
    return;
  }

  try {
    setRevokingInviteId(inviteId);
    setInviteError(null);

    await revokeServerInvite(
      server.id,
      inviteId,
      accessToken
    );

    setInvites(currentInvites =>
      currentInvites.filter(
        invite =>
          invite.id !== inviteId
      )
    );
  } catch (revokeError) {
    setInviteError(
      getErrorMessage(revokeError)
    );
  } finally {
    setRevokingInviteId(null);
  }
};
const handleUnbanMember = async (
  bannedUserId: number
) => {
  if (unbanningUserId !== null) {
    return;
  }

  try {
    setUnbanningUserId(bannedUserId);
    setBanError(null);

    await unbanServerMember(
      server.id,
      bannedUserId,
      accessToken
    );

    setBans(currentBans =>
      currentBans.filter(
        ban =>
          ban.userId !== bannedUserId
      )
    );
  } catch (unbanError) {
    setBanError(
      getErrorMessage(unbanError)
    );
  } finally {
    setUnbanningUserId(null);
  }
};
const handleSelectRole = (
  role: ServerRoleResponse
) => {
  setSelectedRoleId(role.id);
  setRoleName(role.name);
  setRoleColorHex(role.colorHex ?? "");
  setRolePermissions(role.permissions);
  setRoleDisplayedSeparately(
    role.isDisplayedSeparately
  );
  setRoleMentionable(role.isMentionable);
  setRoleError(null);
};

const handleStartCreatingRole = () => {
  setSelectedRoleId(null);
  setRoleName("New Role");
  setRoleColorHex("");
  setRolePermissions([]);
  setRoleDisplayedSeparately(false);
  setRoleMentionable(false);
  setRoleError(null);
};

const handleToggleRolePermission = (
  permission: number
) => {
  setRolePermissions(
    currentPermissions =>
      currentPermissions.includes(permission)
        ? currentPermissions.filter(
            currentPermission =>
              currentPermission !== permission
          )
        : [
            ...currentPermissions,
            permission,
          ]
  );
};
const handleToggleRoleMember = async (
  member: ServerMemberResponse
) => {
  if (
    selectedRoleId === null ||
    changingRoleMemberId !== null
  ) {
    return;
  }

  const selectedRole = roles.find(
    role => role.id === selectedRoleId
  );

  if (
    !selectedRole ||
    selectedRole.isDefault
  ) {
    return;
  }

  const isAssigned = member.roles.some(
    role => role.id === selectedRoleId
  );

  try {
    setChangingRoleMemberId(
      member.userId
    );

    setRoleMemberError(null);

    if (isAssigned) {
      await removeServerRole(
        server.id,
        selectedRoleId,
        member.userId,
        accessToken
      );
    } else {
      await assignServerRole(
        server.id,
        selectedRoleId,
        member.userId,
        accessToken
      );
    }

    const refreshedMembers =
      await getServerMembers(
        server.id,
        accessToken
      );

    setRoleMembers(refreshedMembers);
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
  } catch (memberRoleError) {
    setRoleMemberError(
      getErrorMessage(memberRoleError)
    );
  } finally {
    setChangingRoleMemberId(null);
  }
};
const handleSaveRole = async () => {
  const normalizedName = roleName.trim();

  if (!normalizedName || isSavingRole) {
    return;
  }

  const request = {
    name: normalizedName,
    colorHex: roleColorHex.trim() || null,
    isDisplayedSeparately: roleDisplayedSeparately,
    isMentionable: roleMentionable,
    permissions: rolePermissions,
  };

  try {
    setIsSavingRole(true);
    setRoleError(null);
setIsRoleSaved(false);
    if (selectedRoleId === null) {
      const createdRole = await createServerRole(
        server.id,
        request,
        accessToken
      );

      setRoles(currentRoles =>
        [...currentRoles, createdRole].sort(
          (firstRole, secondRole) =>
            secondRole.position - firstRole.position
        )
      );

handleSelectRole(createdRole);
setIsRoleSaved(true);

window.setTimeout(() => {
  setIsRoleSaved(false);
}, 2000);

return;
    }

    const updatedRole = await updateServerRole(
      server.id,
      selectedRoleId,
      request,
      accessToken
    );

    setRoles(currentRoles =>
      currentRoles
        .map(role =>
          role.id === updatedRole.id
            ? updatedRole
            : role
        )
        .sort(
          (firstRole, secondRole) =>
            secondRole.position - firstRole.position
        )
    );

    handleSelectRole(updatedRole);
    setIsRoleSaved(true);

window.setTimeout(() => {
  setIsRoleSaved(false);
}, 2000);
  } catch (saveRoleError) {
    setRoleError(
      getErrorMessage(saveRoleError)
    );
  } finally {
    setIsSavingRole(false);
  }
};
const handleDeleteRole = async () => {
  const selectedRole = roles.find(
    role => role.id === selectedRoleId
  );

  if (
    !selectedRole ||
    selectedRole.isDefault ||
    deletingRoleId !== null
  ) {
    return;
  }

  const shouldDelete = window.confirm(
    `"${selectedRole.name}" rolunu silmək istəyirsiniz?`
  );

  if (!shouldDelete) {
    return;
  }

  try {
    setDeletingRoleId(selectedRole.id);
    setRoleError(null);

    await deleteServerRole(
      server.id,
      selectedRole.id,
      accessToken
    );

    const remainingRoles = roles.filter(
      role => role.id !== selectedRole.id
    );

    setRoles(remainingRoles);

    const nextRole =
      remainingRoles[0] ?? null;

    if (nextRole) {
      handleSelectRole(nextRole);
    } else {
      handleStartCreatingRole();
    }
  } catch (deleteRoleError) {
    setRoleError(
      getErrorMessage(deleteRoleError)
    );
  } finally {
    setDeletingRoleId(null);
  }
};

  const handleSubmit = async (
    event: FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault();

    const normalizedName =
      name.trim();

    if (
      !normalizedName ||
      isSaving
    ) {
      return;
    }

    try {
      setIsSaving(true);
      setError(null);

      const updatedServer =
        await updateServer(
          server.id,
          {
            name: normalizedName,
            description:
              description.trim() ||
              null,
            isPublic,
          },
          accessToken
        );

      onServerUpdated(
        updatedServer
      );

      if (selectedIcon) {
        const serverWithNewIcon =
          await uploadServerIcon(
            server.id,
            selectedIcon,
            accessToken
          );

        onServerUpdated(
          serverWithNewIcon
        );
      }

      onClose();
    } catch (saveError) {
      setError(
        getErrorMessage(saveError)
      );
    } finally {
      setIsSaving(false);
    }
  };

  if (
    !open ||
    typeof document === "undefined"
  ) {
    return null;
  }

  const iconSource =
    iconPreview ??
    getServerIconSource(
      server.iconUrl
    );
const selectedRole = roles.find(
  role => role.id === selectedRoleId
) ?? null;

const permissionCategories = Array.from(
  new Set(
    SERVER_PERMISSION_OPTIONS.map(
      option => option.category
    )
  )
);

const isDefaultRole =
  selectedRole?.isDefault ?? false;
  return createPortal(
    <div
      role="presentation"
      onMouseDown={onClose}
      className="fixed inset-0 z-[120] flex items-center justify-center bg-black/75 px-4"
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Server Settings"
        onMouseDown={event =>
          event.stopPropagation()
        }
        className="relative w-full max-w-[560px] rounded-xl bg-[#313338] p-6 text-white shadow-2xl"
      >
        <button
          type="button"
          aria-label="Close"
          onClick={onClose}
          className="absolute right-4 top-3 text-2xl text-gray-400 hover:text-white"
        >
          ×
        </button>

        <h2 className="text-xl font-bold">
          Server Settings
        </h2>

        <p className="mt-1 text-sm text-gray-400">
          Customize your server&apos;s
          identity and visibility.
        </p>

        <div className="mt-5 flex border-b border-white/10">
  <button
    type="button"
    onClick={() =>
      setActiveSection("overview")
    }
    className={`border-b-2 px-4 py-2 text-sm font-semibold ${
      activeSection === "overview"
        ? "border-primary text-white"
        : "border-transparent text-gray-400 hover:text-gray-200"
    }`}
  >
    Overview
  </button>
<button
  type="button"
  onClick={() =>
    setActiveSection("roles")
  }
  className={`border-b-2 px-4 py-2 text-sm font-semibold ${
    activeSection === "roles"
      ? "border-primary text-white"
      : "border-transparent text-gray-400 hover:text-gray-200"
  }`}
>
  Roles
</button>
<button
  type="button"
  onClick={() =>
    setActiveSection("bans")
  }
  className={`border-b-2 px-4 py-2 text-sm font-semibold ${
    activeSection === "bans"
      ? "border-primary text-white"
      : "border-transparent text-gray-400 hover:text-gray-200"
  }`}
>
  Bans
</button>
  <button
    type="button"
    onClick={() =>
      setActiveSection("invites")
    }
    className={`border-b-2 px-4 py-2 text-sm font-semibold ${
      activeSection === "invites"
        ? "border-primary text-white"
        : "border-transparent text-gray-400 hover:text-gray-200"
    }`}
  >
    Invites
  </button>
</div>
{activeSection === "overview" && (
        <form
          onSubmit={handleSubmit}
          className="mt-6"
        >
          <div className="flex flex-col items-center">
            <div className="relative flex h-24 w-24 items-center justify-center overflow-hidden rounded-full bg-primary text-3xl font-bold">
              {iconSource ? (
                <Image
                  src={iconSource}
                  alt={`${server.name} icon`}
                  fill
                  unoptimized
                  sizes="96px"
                  className="object-cover"
                />
              ) : (
                server.name
                  .charAt(0)
                  .toUpperCase()
              )}
            </div>

            <label
              htmlFor="settings-server-icon"
              className="mt-3 cursor-pointer text-sm font-semibold text-primary hover:underline"
            >
              Change Icon
            </label>

            <input
              id="settings-server-icon"
              type="file"
              accept=".jpg,.jpeg,.png,.webp"
              onChange={handleIconChange}
              className="hidden"
            />

            <p className="mt-1 text-xs text-gray-400">
              JPG, PNG or WEBP · Max 5 MB
            </p>
          </div>

          <label className="mt-6 block text-xs font-bold uppercase text-gray-300">
            Server name
          </label>

          <input
            value={name}
            onChange={event =>
              setName(
                event.target.value
              )
            }
            maxLength={100}
            className="mt-2 w-full rounded-md bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-primary"
          />

          <label className="mt-4 block text-xs font-bold uppercase text-gray-300">
            Description
          </label>

          <textarea
            value={description}
            onChange={event =>
              setDescription(
                event.target.value
              )
            }
            maxLength={500}
            rows={3}
            className="mt-2 w-full resize-none rounded-md bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-primary"
          />

          <label className="mt-4 flex cursor-pointer items-center gap-3 text-sm">
            <input
              type="checkbox"
              checked={isPublic}
              onChange={event =>
                setIsPublic(
                  event.target.checked
                )
              }
              className="h-4 w-4 accent-primary"
            />

            Make this server public
          </label>

          {error && (
            <p className="mt-4 text-sm text-red-400">
              {error}
            </p>
          )}

          <div className="mt-6 flex justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              disabled={isSaving}
              className="px-4 py-2 text-sm font-semibold text-gray-300 hover:underline disabled:opacity-50"
            >
              Cancel
            </button>

            <button
              type="submit"
              disabled={
                isSaving ||
                !name.trim()
              }
              className="rounded-md bg-primary px-5 py-2.5 text-sm font-semibold hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isSaving
                ? "Saving..."
                : "Save Changes"}
            </button>
          </div>
        </form>)}
{activeSection === "roles" && (
  <div className="mt-6">
    {roleError && (
      <p className="mb-4 rounded-md bg-red-500/10 px-3 py-2 text-sm text-red-400">
        {roleError}
      </p>
    )}

    {isLoadingRoles ? (
      <p className="py-8 text-center text-sm text-gray-400">
        Roles are loading...
      </p>
    ) : (
      <div className="flex min-h-[280px] gap-4">
        <aside className="w-44 flex-none border-r border-white/10 pr-3">
          <button
            type="button"
            onClick={handleStartCreatingRole}
            className="mb-3 w-full rounded-md bg-primary px-3 py-2 text-sm font-semibold hover:brightness-110"
          >
            + Create Role
          </button>

          <div className="space-y-1">
            {roles.map(role => (
              <button
                key={role.id}
                type="button"
                onClick={() =>
                  handleSelectRole(role)
                }
                className={`flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-sm ${
                  selectedRoleId === role.id
                    ? "bg-white/10 text-white"
                    : "text-gray-300 hover:bg-white/5"
                }`}
              >
                <span
                  className="h-3 w-3 flex-none rounded-full"
                  style={{
                    backgroundColor:
                      role.colorHex ?? "#99AAB5",
                  }}
                />

                <span className="min-w-0 flex-1 truncate">
                  {role.name}
                </span>
              </button>
            ))}
          </div>
        </aside>

      <section className="min-w-0 flex-1 max-h-[55vh] overflow-y-auto pr-2">
  <div className="flex items-start justify-between gap-3">
    <div>
      <h3 className="font-semibold text-white">
        {selectedRoleId === null
          ? "Create Role"
          : `Edit ${selectedRole?.name ?? "Role"}`}
      </h3>

      <p className="mt-1 text-xs text-gray-400">
        Rolun görünüşünü və icazələrini idarə et.
      </p>
    </div>

    
  </div>

  <label className="mt-5 block text-xs font-bold uppercase text-gray-300">
    Role name
  </label>

  <input
    value={roleName}
    onChange={event =>
      setRoleName(event.target.value)
    }
    disabled={isDefaultRole}
    maxLength={100}
    className="mt-2 w-full rounded-md bg-[#1e1f22] px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-primary disabled:cursor-not-allowed disabled:opacity-50"
  />

  <label className="mt-4 block text-xs font-bold uppercase text-gray-300">
    Role color
  </label>

  <div className="mt-2 flex items-center gap-3">
    <input
      type="color"
      value={
        /^#[0-9A-Fa-f]{6}$/.test(
          roleColorHex
        )
          ? roleColorHex
          : "#99AAB5"
      }
      onChange={event =>
        setRoleColorHex(
          event.target.value.toUpperCase()
        )
      }
      disabled={isDefaultRole}
      className="h-10 w-14 cursor-pointer rounded bg-transparent disabled:cursor-not-allowed disabled:opacity-50"
    />

    <input
      value={roleColorHex}
      onChange={event =>
        setRoleColorHex(
          event.target.value
        )
      }
      placeholder="#5865F2"
      disabled={isDefaultRole}
      maxLength={7}
      className="min-w-0 flex-1 rounded-md bg-[#1e1f22] px-3 py-2.5 text-sm uppercase outline-none focus:ring-2 focus:ring-primary disabled:cursor-not-allowed disabled:opacity-50"
    />
  </div>

  <label className="mt-4 flex cursor-pointer items-center gap-3 text-sm">
    <input
      type="checkbox"
      checked={roleDisplayedSeparately}
      onChange={event =>
        setRoleDisplayedSeparately(
          event.target.checked
        )
      }
      disabled={isDefaultRole}
      className="h-4 w-4 accent-primary disabled:opacity-50"
    />

    Display role members separately
  </label>

  <label className="mt-3 flex cursor-pointer items-center gap-3 text-sm">
    <input
      type="checkbox"
      checked={roleMentionable}
      onChange={event =>
        setRoleMentionable(
          event.target.checked
        )
      }
      disabled={isDefaultRole}
      className="h-4 w-4 accent-primary disabled:opacity-50"
    />

    Allow anyone to mention this role
  </label>
<div className="mt-6 border-t border-white/10 pt-5">
  <h3 className="font-semibold">
    Manage Members
  </h3>

  <p className="mt-1 text-xs text-gray-400">
    Bu rola sahib olacaq server üzvlərini seç.
  </p>

  {selectedRoleId === null ? (
    <p className="mt-4 rounded-md bg-[#2b2d31] px-3 py-3 text-sm text-gray-400">
      Üzvləri əlavə etmək üçün əvvəlcə rolu yarat.
    </p>
  ) : isDefaultRole ? (
    <p className="mt-4 rounded-md bg-[#2b2d31] px-3 py-3 text-sm text-gray-400">
      @everyone rolu bütün server üzvlərinə avtomatik verilir.
    </p>
  ) : (
    <>
      {roleMemberError && (
        <p className="mt-4 rounded-md bg-red-500/10 px-3 py-2 text-sm text-red-400">
          {roleMemberError}
        </p>
      )}

      {isLoadingRoleMembers ? (
        <p className="mt-4 text-sm text-gray-400">
          Members are loading...
        </p>
      ) : (
        <div className="mt-4 max-h-52 space-y-2 overflow-y-auto pr-1">
          {roleMembers.map(member => {
            const isAssigned =
              member.roles.some(
                role =>
                  role.id === selectedRoleId
              );

            const isChanging =
              changingRoleMemberId ===
              member.userId;

            return (
              <button
                key={member.userId}
                type="button"
                disabled={
                  changingRoleMemberId !==
                  null
                }
                onClick={() =>
                  void handleToggleRoleMember(
                    member
                  )
                }
                className="flex w-full items-center justify-between rounded-md bg-[#2b2d31] px-3 py-3 text-left text-sm hover:bg-[#35373c] disabled:cursor-wait disabled:opacity-60"
              >
                <span className="min-w-0 truncate text-gray-200">
                  {member.nickname?.trim() ||
                    member.displayName}
                </span>

                <span
                  className={`flex h-5 w-5 items-center justify-center rounded border ${
                    isAssigned
                      ? "border-primary bg-primary text-white"
                      : "border-gray-500 text-transparent"
                  }`}
                >
                  {isChanging
                    ? "…"
                    : isAssigned
                      ? "✓"
                      : ""}
                </span>
              </button>
            );
          })}
        </div>
      )}
    </>
  )}
</div>
  <div className="mt-6 border-t border-white/10 pt-5">
    <h3 className="font-semibold">
      Permissions
    </h3>

    <p className="mt-1 text-xs text-gray-400">
      Bu rola sahib üzvlərin edə biləcəyi əməliyyatları seç.
    </p>

    <div className="mt-4 space-y-5">
      {permissionCategories.map(
        category => (
          <div key={category}>
            <h4 className="mb-2 text-xs font-bold uppercase text-gray-400">
              {category}
            </h4>

            <div className="space-y-2">
              {SERVER_PERMISSION_OPTIONS
                .filter(
                  option =>
                    option.category ===
                    category
                )
                .map(option => (
                  <label
                    key={option.value}
                    className="flex cursor-pointer items-center justify-between rounded-md bg-[#2b2d31] px-3 py-2.5 text-sm hover:bg-[#35373c]"
                  >
                    <span>
                      {option.label}
                    </span>

                    <input
                      type="checkbox"
                      checked={rolePermissions.includes(
                        option.value
                      )}
                      onChange={() =>
                        handleToggleRolePermission(
                          option.value
                        )
                      }
                      className="h-4 w-4 accent-primary"
                    />
                  </label>
                ))}
            </div>
          </div>
        )
      )}
    </div>
  </div>

 <div className="sticky bottom-0 mt-6 flex items-center justify-between border-t border-white/10 bg-[#313338] py-4">
  <div>
    {selectedRole &&
      !selectedRole.isDefault && (
        <button
          type="button"
          onClick={() =>
            void handleDeleteRole()
          }
          disabled={
            deletingRoleId !== null ||
            isSavingRole
          }
          className="rounded-md px-4 py-2.5 text-sm font-semibold text-red-400 transition hover:bg-red-500/10 hover:text-red-300 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {deletingRoleId ===
          selectedRole.id
            ? "Deleting..."
            : "Delete Role"}
        </button>
      )}
  </div>

  <button
    type="button"
    onClick={() =>
      void handleSaveRole()
    }
    disabled={
      isSavingRole ||
      deletingRoleId !== null ||
      !roleName.trim()
    }
    className="rounded-md bg-primary px-5 py-2.5 text-sm font-semibold hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-50"
  >
   {isSavingRole
  ? "Saving..."
  : isRoleSaved
    ? "Saved ✓"
    : selectedRoleId === null
      ? "Create Role"
      : "Save Changes"}
  </button>
</div>
</section>
      </div>
    )}
  </div>
)}
{activeSection === "bans" && (
  <div className="mt-6">
    <div>
      <h3 className="font-semibold text-white">
        Server Bans
      </h3>

      <p className="mt-1 text-sm text-gray-400">
        Bu serverdən ban edilmiş istifadəçiləri idarə et.
      </p>
    </div>

    {banError && (
      <p className="mt-4 rounded-md bg-red-500/10 px-3 py-2 text-sm text-red-400">
        {banError}
      </p>
    )}

    {isLoadingBans ? (
      <p className="py-8 text-center text-sm text-gray-400">
        Bans are loading...
      </p>
    ) : bans.length === 0 ? (
      <div className="mt-5 rounded-md bg-[#2b2d31] px-4 py-5 text-center">
        <p className="text-sm font-medium text-gray-300">
          No banned members
        </p>

        <p className="mt-1 text-xs text-gray-500">
          Bu serverdə hazırda ban edilmiş istifadəçi yoxdur.
        </p>
      </div>
    ) : (
      <div className="mt-5 max-h-[55vh] space-y-2 overflow-y-auto pr-1">
        {bans.map(ban => (
          <div
            key={ban.id}
            className="flex items-center gap-3 rounded-md bg-[#2b2d31] p-3"
          >
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-semibold text-white">
                {ban.displayName}
              </p>

              <p className="truncate text-xs text-gray-500">
                @{ban.username}
              </p>

              {ban.reason && (
                <p className="mt-2 text-xs text-gray-400">
                  Reason: {ban.reason}
                </p>
              )}
            </div>

            <button
              type="button"
              disabled={
                unbanningUserId !== null
              }
              onClick={() =>
                void handleUnbanMember(
                  ban.userId
                )
              }
              className="flex-none rounded-md bg-[#4e5058] px-3 py-2 text-xs font-semibold text-white hover:bg-[#5d6069] disabled:cursor-wait disabled:opacity-50"
            >
              {unbanningUserId ===
              ban.userId
                ? "Unbanning..."
                : "Unban"}
            </button>
          </div>
        ))}
      </div>
    )}
  </div>
)}
{activeSection === "invites" && (
  <div className="mt-6">
    <div className="rounded-lg bg-[#2b2d31] p-4">
      <h3 className="font-semibold">
        Create an Invite
      </h3>

      <p className="mt-1 text-sm text-gray-400">
        Create an invitation code for
        people you want to add.
      </p>

      <div className="mt-4 grid grid-cols-2 gap-3">
        <div>
          <label className="block text-xs font-bold uppercase text-gray-300">
            Expiration hours
          </label>

          <input
            type="number"
            min={1}
            max={168}
            value={expirationHours}
            onChange={event =>
              setExpirationHours(
                event.target.value
              )
            }
            className="mt-2 w-full rounded-md bg-[#1e1f22] px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-primary"
          />
        </div>

        <div>
          <label className="block text-xs font-bold uppercase text-gray-300">
            Max uses
          </label>

          <input
            type="number"
            min={1}
            max={100}
            value={maxUses}
            onChange={event =>
              setMaxUses(
                event.target.value
              )
            }
            className="mt-2 w-full rounded-md bg-[#1e1f22] px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-primary"
          />
        </div>
      </div>

      <button
        type="button"
        onClick={
          handleCreateInvite
        }
        disabled={isCreatingInvite}
        className="mt-4 w-full rounded-md bg-primary px-4 py-2.5 text-sm font-semibold hover:brightness-110 disabled:cursor-wait disabled:opacity-50"
      >
        {isCreatingInvite
          ? "Creating..."
          : "Create Invite"}
      </button>
    </div>

    {inviteError && (
      <p className="mt-3 rounded-md bg-red-500/10 px-3 py-2 text-sm text-red-400">
        {inviteError}
      </p>
    )}

    <div className="mt-5">
      <h3 className="text-sm font-semibold uppercase text-gray-300">
        Existing Invites
      </h3>

      {isLoadingInvites && (
        <p className="mt-3 text-sm text-gray-400">
          Loading invites...
        </p>
      )}

      {!isLoadingInvites &&
        invites.length === 0 && (
          <p className="mt-3 rounded-md bg-[#2b2d31] px-4 py-3 text-sm text-gray-400">
            No invites have been
            created yet.
          </p>
        )}

      {!isLoadingInvites &&
        invites.length > 0 && (
          <div className="mt-3 max-h-52 space-y-2 overflow-y-auto pr-1">
            {invites.map(invite => (
              <div
                key={invite.id}
                className="flex items-center gap-3 rounded-md bg-[#2b2d31] p-3"
              >
                <div className="min-w-0 flex-1">
                  <p className="truncate font-mono text-sm font-semibold text-white">
                    {invite.code}
                  </p>

                  <p className="mt-1 text-xs text-gray-400">
                    Uses: {invite.uses}
                    {invite.maxUses !== null
                      ? ` / ${invite.maxUses}`
                      : " / Unlimited"}
                  </p>

                  <p className="mt-1 text-xs text-gray-500">
                    {invite.expiresAt
                      ? `Expires: ${new Date(
                          invite.expiresAt
                        ).toLocaleString()}`
                      : "Never expires"}
                  </p>
                </div>

                <button
                  type="button"
                  onClick={() =>
                    void handleCopyInvite(
                      invite.code
                    )
                  }
                  className="rounded-md bg-[#4e5058] px-3 py-2 text-xs font-semibold hover:bg-[#5d6069]"
                >
                  {copiedCode ===
                  invite.code
                    ? "Copied!"
                    : "Copy Code"}
                </button>

                <button
                  type="button"
                  onClick={() =>
                    void handleRevokeInvite(
                      invite.id
                    )
                  }
                  disabled={
                    revokingInviteId ===
                    invite.id
                  }
                  className="rounded-md px-3 py-2 text-xs font-semibold text-red-400 hover:bg-red-500/10 disabled:opacity-50"
                >
                  {revokingInviteId ===
                  invite.id
                    ? "Revoking..."
                    : "Revoke"}
                </button>
              </div>
            ))}
          </div>
        )}
    </div>
  </div>
)}
      </div>
    </div>,
    document.body
  );
}