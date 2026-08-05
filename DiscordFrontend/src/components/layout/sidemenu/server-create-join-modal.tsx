"use client";

import {
  type FormEvent,
  useEffect,
  useState,
} from "react";

import { createPortal } from "react-dom";
import { useRouter } from "next/navigation";
import Image from "next/image";

import {
  createServer,
  getServerInviteDetails,
  joinServer,
  uploadServerIcon,
} from "@/lib/serverApi";

import type {
  ServerInviteDetailsResponse,
} from "@/lib/serverApi";

type ModalPage =
  | "home"
  | "create"
  | "join";

interface ServerCreateJoinModalProps {
  open: boolean;
  accessToken: string | null;
  onClose: () => void;
  onServersChanged: () => void;
}

function getErrorMessage(
  error: unknown
): string {
  return error instanceof Error
    ? error.message
    : "The operation could not be completed.";
}

function extractInviteCode(
  value: string
): string {
  const trimmedValue = value.trim();

  if (!trimmedValue) {
    return "";
  }

  try {
    const url = new URL(trimmedValue);
    const segments = url.pathname
      .split("/")
      .filter(Boolean);

    return (
      segments.at(-1) ?? ""
    );
  } catch {
    const segments = trimmedValue
      .split("/")
      .filter(Boolean);

    return (
      segments.at(-1) ?? trimmedValue
    );
  }
}

export default function ServerCreateJoinModal({
  open,
  accessToken,
  onClose,
  onServersChanged,
}: ServerCreateJoinModalProps) {
  const router = useRouter();

  const [page, setPage] =
    useState<ModalPage>("home");

  const [serverName, setServerName] =
    useState("");

  const [
    serverDescription,
    setServerDescription,
  ] = useState("");

  const [
  serverIcon,
  setServerIcon,
] = useState<File | null>(null);

const [
  serverIconPreview,
  setServerIconPreview,
] = useState<string | null>(null);
  const [isPublic, setIsPublic] =
    useState(false);

  const [inviteValue, setInviteValue] =
    useState("");

  const [
    inviteDetails,
    setInviteDetails,
  ] = useState<
    ServerInviteDetailsResponse | null
  >(null);

  const [isLoading, setIsLoading] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    setPage("home");
    setServerName("");
setServerDescription("");
setServerIcon(null);
setServerIconPreview(null);
setIsPublic(false);
    setInviteValue("");
    setInviteDetails(null);
    setError(null);
    setIsLoading(false);

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
    if (serverIconPreview) {
      URL.revokeObjectURL(
        serverIconPreview
      );
    }
  };
}, [serverIconPreview]);

const handleServerIconChange = (
  event: React.ChangeEvent<HTMLInputElement>
) => {
  const file =
    event.target.files?.[0] ?? null;

  setError(null);

  if (!file) {
    setServerIcon(null);
    setServerIconPreview(null);
    return;
  }

  const allowedTypes = [
    "image/jpeg",
    "image/png",
    "image/webp",
  ];

  if (!allowedTypes.includes(file.type)) {
    event.target.value = "";

    setServerIcon(null);
    setServerIconPreview(null);

    setError(
      "Only JPG, PNG and WEBP images are allowed."
    );

    return;
  }

  const maximumSize =
    5 * 1024 * 1024;

  if (file.size > maximumSize) {
    event.target.value = "";

    setServerIcon(null);
    setServerIconPreview(null);

    setError(
      "The server icon must not exceed 5 MB."
    );

    return;
  }

  setServerIcon(file);

  setServerIconPreview(
    URL.createObjectURL(file)
  );
};

  const handleCreateServer = async (
  event: FormEvent<HTMLFormElement>
) => {
  event.preventDefault();

  const name = serverName.trim();

  if (
    !name ||
    !accessToken ||
    isLoading
  ) {
    return;
  }

  try {
    setIsLoading(true);
    setError(null);

    const createdServer =
      await createServer(
        {
          name,
          description:
            serverDescription.trim() ||
            null,
          isPublic,
        },
        accessToken
      );

    if (serverIcon) {
      await uploadServerIcon(
        createdServer.id,
        serverIcon,
        accessToken
      );
    }

    onServersChanged();
    onClose();

    router.push(
      `/servers/${createdServer.id}`
    );
  } catch (createError) {
    setError(
      getErrorMessage(createError)
    );
  } finally {
    setIsLoading(false);
  }
};

  const handleCheckInvite = async (
    event: FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault();

    const code =
      extractInviteCode(inviteValue);

    if (
      !code ||
      !accessToken ||
      isLoading
    ) {
      return;
    }

    try {
      setIsLoading(true);
      setError(null);

      const details =
        await getServerInviteDetails(
          code,
          accessToken
        );

      setInviteDetails(details);
    } catch (inviteError) {
      setInviteDetails(null);

      setError(
        getErrorMessage(inviteError)
      );
    } finally {
      setIsLoading(false);
    }
  };

  const handleJoinServer = async () => {
    if (
      !inviteDetails ||
      !accessToken ||
      isLoading
    ) {
      return;
    }

    try {
      setIsLoading(true);
      setError(null);

      if (
        !inviteDetails.isAlreadyMember
      ) {
        await joinServer(
          inviteDetails.code,
          accessToken
        );
      }

      onServersChanged();
      onClose();

      router.push(
        `/servers/${inviteDetails.serverId}`
      );
    } catch (joinError) {
      setError(
        getErrorMessage(joinError)
      );
    } finally {
      setIsLoading(false);
    }
  };

  if (
    !open ||
    typeof document === "undefined"
  ) {
    return null;
  }

  return createPortal(
    <div
      role="presentation"
      onMouseDown={onClose}
      className="fixed inset-0 z-[100] flex items-center justify-center bg-black/70 px-4"
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Create or join a server"
        onMouseDown={event =>
          event.stopPropagation()
        }
        className="relative w-full max-w-[440px] rounded-xl bg-[#313338] p-6 text-white shadow-2xl"
      >
        <button
          type="button"
          aria-label="Close"
          onClick={onClose}
          className="absolute right-4 top-3 text-2xl text-gray-400 hover:text-white"
        >
          ×
        </button>

        {page !== "home" && (
          <button
            type="button"
            onClick={() => {
              setPage("home");
              setError(null);
              setInviteDetails(null);
            }}
            className="mb-4 text-sm text-gray-400 hover:text-white"
          >
            ← Back
          </button>
        )}

        {page === "home" && (
          <>
            <h2 className="text-center text-2xl font-bold">
              Create or Join a Server
            </h2>

            <p className="mt-2 text-center text-sm text-gray-400">
              Your server is where you
              hang out with your
              community.
            </p>

            <button
              type="button"
              onClick={() =>
                setPage("create")
              }
              className="mt-6 w-full rounded-lg border border-gray-600 bg-[#2b2d31] p-4 text-left hover:bg-[#35373c]"
            >
              <span className="block font-semibold">
                Create My Own
              </span>

              <span className="mt-1 block text-sm text-gray-400">
                Start a new community or
                private server.
              </span>
            </button>

            <div className="mt-6 text-center">
              <p className="mb-3 font-semibold">
                Have an invite already?
              </p>

              <button
                type="button"
                onClick={() =>
                  setPage("join")
                }
                className="w-full rounded-md bg-[#4e5058] px-4 py-2.5 font-semibold hover:bg-[#5d6069]"
              >
                Join a Server
              </button>
            </div>
          </>
        )}

        {page === "create" && (
          <form
            onSubmit={handleCreateServer}
          >
            <h2 className="text-center text-2xl font-bold">
              Create Your Server
            </h2>

            <p className="mt-2 text-center text-sm text-gray-400">
              Give your new server a
              personality with a name.
            </p>

            <div className="mt-6 flex flex-col items-center">
  <label
    htmlFor="server-icon"
    className="group relative flex h-20 w-20 cursor-pointer items-center justify-center overflow-hidden rounded-full border-2 border-dashed border-gray-500 bg-[#2b2d31] transition hover:border-white"
  >
    {serverIconPreview ? (
      <Image
        src={serverIconPreview}
        alt="Server icon preview"
        fill
        unoptimized
        sizes="80px"
        className="object-cover"
      />
    ) : (
      <span className="px-2 text-center text-xs font-bold uppercase text-gray-400 group-hover:text-white">
        Upload Icon
      </span>
    )}
  </label>

  <input
    id="server-icon"
    type="file"
    accept=".jpg,.jpeg,.png,.webp"
    onChange={
      handleServerIconChange
    }
    className="hidden"
  />

  <p className="mt-2 text-xs text-gray-400">
    JPG, PNG or WEBP · Max 5 MB
  </p>

  {serverIcon && (
    <button
      type="button"
      onClick={() => {
        setServerIcon(null);
        setServerIconPreview(null);
      }}
      className="mt-1 text-xs text-red-400 hover:text-red-300"
    >
      Remove selected icon
    </button>
  )}
</div>

            <label className="mt-6 block text-xs font-bold uppercase text-gray-300">
              Server name
            </label>

            <input
              value={serverName}
              onChange={event =>
                setServerName(
                  event.target.value
                )
              }
              maxLength={100}
              autoFocus
              className="mt-2 w-full rounded-md bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-primary"
              placeholder="Enter a server name"
            />

            <label className="mt-4 block text-xs font-bold uppercase text-gray-300">
              Description
            </label>

            <textarea
              value={serverDescription}
              onChange={event =>
                setServerDescription(
                  event.target.value
                )
              }
              maxLength={500}
              rows={3}
              className="mt-2 w-full resize-none rounded-md bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-primary"
              placeholder="What is this server about?"
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

            <button
              type="submit"
              disabled={
                !serverName.trim() ||
                isLoading
              }
              className="mt-6 w-full rounded-md bg-primary px-4 py-3 font-semibold hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isLoading
                ? "Creating..."
                : "Create Server"}
            </button>
          </form>
        )}

        {page === "join" && (
          <>
            <h2 className="text-center text-2xl font-bold">
              Join a Server
            </h2>

            <p className="mt-2 text-center text-sm text-gray-400">
              Enter an invite link or
              invite code below.
            </p>

            {!inviteDetails && (
              <form
                onSubmit={
                  handleCheckInvite
                }
              >
                <label className="mt-6 block text-xs font-bold uppercase text-gray-300">
                  Invite link
                </label>

                <input
                  value={inviteValue}
                  onChange={event => {
                    setInviteValue(
                      event.target.value
                    );

                    setError(null);
                  }}
                  autoFocus
                  className="mt-2 w-full rounded-md bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-primary"
                  placeholder="Enter an invite code"
                />

                {error && (
                  <p className="mt-4 text-sm text-red-400">
                    {error}
                  </p>
                )}

                <button
                  type="submit"
                  disabled={
                    !inviteValue.trim() ||
                    isLoading
                  }
                  className="mt-6 w-full rounded-md bg-primary px-4 py-3 font-semibold hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {isLoading
                    ? "Checking..."
                    : "Continue"}
                </button>
              </form>
            )}

            {inviteDetails && (
              <div className="mt-6 rounded-lg bg-[#2b2d31] p-4 text-center">
                <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-primary text-2xl font-bold">
                  {inviteDetails.serverName
                    .charAt(0)
                    .toUpperCase()}
                </div>

                <h3 className="mt-3 text-lg font-bold">
                  {
                    inviteDetails.serverName
                  }
                </h3>

                {inviteDetails.serverDescription && (
                  <p className="mt-1 text-sm text-gray-400">
                    {
                      inviteDetails.serverDescription
                    }
                  </p>
                )}

                <p className="mt-3 text-sm text-gray-400">
                  {
                    inviteDetails.memberCount
                  }{" "}
                  members · Invited by{" "}
                  {
                    inviteDetails.createdByDisplayName
                  }
                </p>

                {error && (
                  <p className="mt-3 text-sm text-red-400">
                    {error}
                  </p>
                )}

                <button
                  type="button"
                  onClick={
                    handleJoinServer
                  }
                  disabled={isLoading}
                  className="mt-5 w-full rounded-md bg-primary px-4 py-3 font-semibold hover:brightness-110 disabled:opacity-50"
                >
                  {isLoading
                    ? "Please wait..."
                    : inviteDetails.isAlreadyMember
                      ? "Open Server"
                      : "Join Server"}
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </div>,
    document.body
  );
}