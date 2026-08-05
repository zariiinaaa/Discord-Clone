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
  revokeServerInvite,
  updateServer,
  uploadServerIcon,
} from "@/lib/serverApi";

import type {
  ServerDetailsResponse,
  ServerInviteResponse,
  ServerResponse,
} from "@/lib/serverApi";

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
  useState<"overview" | "invites">(
    "overview"
  );
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

  try {
    setIsCreatingInvite(true);
    setInviteError(null);

    const createdInvite =
      await createServerInvite(
        server.id,
        {
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