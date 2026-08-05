"use client";

import {
  type ChangeEvent,
  type FormEvent,
  useEffect,
  useState,
} from "react";

import Image from "next/image";
import { createPortal } from "react-dom";

import {
  deleteCurrentUserAvatar,
  mapAuthenticatedUser,
  updateCurrentUserProfile,
  uploadCurrentUserAvatar,
} from "@/lib/userApi";

import { useAuthStore } from "@/state/auth";
import { useCurrentUserStore } from "@/state/user";

interface UserSettingsModalProps {
  open: boolean;
  onClose: () => void;
}

function getErrorMessage(
  error: unknown
): string {
  return error instanceof Error
    ? error.message
    : "Profile could not be updated.";
}

export default function UserSettingsModal({
  open,
  onClose,
}: UserSettingsModalProps) {
  const accessToken = useAuthStore(
    state => state.accessToken
  );

  const {
    currentUser,
    setCurrentUser,
  } = useCurrentUserStore();

  const [displayName, setDisplayName] =
    useState("");

  const [bio, setBio] =
    useState("");

  const [
    selectedAvatar,
    setSelectedAvatar,
  ] = useState<File | null>(null);

  const [
    avatarPreview,
    setAvatarPreview,
  ] = useState<string | null>(null);

  const [isSaving, setIsSaving] =
    useState(false);

  const [
    isDeletingAvatar,
    setIsDeletingAvatar,
  ] = useState(false);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    if (!open || !currentUser) {
      return;
    }

    setDisplayName(
      currentUser.name
    );

    setBio(
      currentUser.bio ?? ""
    );

    setSelectedAvatar(null);
    setAvatarPreview(null);
    setIsSaving(false);
    setIsDeletingAvatar(false);
    setError(null);
  }, [
    open,
    currentUser?.name,
    currentUser?.bio,
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
  }, [
    open,
    onClose,
  ]);

  useEffect(() => {
    return () => {
      if (avatarPreview) {
        URL.revokeObjectURL(
          avatarPreview
        );
      }
    };
  }, [avatarPreview]);

  const handleAvatarChange = (
    event: ChangeEvent<HTMLInputElement>
  ) => {
    const file =
      event.target.files?.[0] ??
      null;

    setError(null);

    if (!file) {
      return;
    }

    const allowedTypes = [
      "image/jpeg",
      "image/png",
      "image/webp",
    ];

    if (
      !allowedTypes.includes(
        file.type
      )
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
        "Avatar must not exceed 5 MB."
      );

      return;
    }

    setSelectedAvatar(file);

    setAvatarPreview(
      URL.createObjectURL(file)
    );
  };

  const handleSubmit = async (
    event: FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault();

    const normalizedDisplayName =
      displayName.trim();

    if (
      !normalizedDisplayName ||
      !accessToken ||
      isSaving
    ) {
      return;
    }

    try {
      setIsSaving(true);
      setError(null);

      const updatedProfile =
        await updateCurrentUserProfile(
          {
            displayName:
              normalizedDisplayName,

            bio:
              bio.trim() || null,
          },
          accessToken
        );

      setCurrentUser(
        mapAuthenticatedUser(
          updatedProfile
        )
      );

      if (selectedAvatar) {
        const userWithNewAvatar =
          await uploadCurrentUserAvatar(
            selectedAvatar,
            accessToken
          );

        setCurrentUser(
          mapAuthenticatedUser(
            userWithNewAvatar
          )
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

  const handleDeleteAvatar =
    async () => {
      if (
        !accessToken ||
        isDeletingAvatar ||
        !window.confirm(
          "Remove your avatar?"
        )
      ) {
        return;
      }

      try {
        setIsDeletingAvatar(true);
        setError(null);

        await deleteCurrentUserAvatar(
          accessToken
        );

        if (currentUser) {
          setCurrentUser({
            ...currentUser,
            avatar: null,
          });
        }

        setSelectedAvatar(null);
        setAvatarPreview(null);
      } catch (deleteError) {
        setError(
          getErrorMessage(
            deleteError
          )
        );
      } finally {
        setIsDeletingAvatar(false);
      }
    };

  if (
    !open ||
    !currentUser ||
    typeof document === "undefined"
  ) {
    return null;
  }

  const avatarSource =
    avatarPreview ??
    currentUser.avatar ??
    null;

  return createPortal(
    <div
      role="presentation"
      onMouseDown={onClose}
      className="fixed inset-0 z-[150] flex items-center justify-center bg-black/75 px-4"
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="User Settings"
        onMouseDown={event =>
          event.stopPropagation()
        }
        className="relative w-full max-w-[520px] rounded-xl bg-[#313338] p-6 text-white shadow-2xl"
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
          User Settings
        </h2>

        <p className="mt-1 text-sm text-gray-400">
          Customize your profile and
          avatar.
        </p>

        <form
          onSubmit={handleSubmit}
          className="mt-6"
        >
          <div className="flex flex-col items-center">
            <div className="relative flex h-24 w-24 items-center justify-center overflow-hidden rounded-full bg-primary text-3xl font-bold">
              {avatarSource ? (
                <Image
                  src={avatarSource}
                  alt={currentUser.name}
                  fill
                  unoptimized
                  sizes="96px"
                  className="object-cover"
                />
              ) : (
                currentUser.name
                  .charAt(0)
                  .toUpperCase()
              )}
            </div>

            <div className="mt-3 flex items-center gap-3">
              <label
                htmlFor="user-settings-avatar"
                className="cursor-pointer text-sm font-semibold text-primary hover:underline"
              >
                Change Avatar
              </label>

              {currentUser.avatar && (
                <button
                  type="button"
                  onClick={() =>
                    void handleDeleteAvatar()
                  }
                  disabled={
                    isDeletingAvatar
                  }
                  className="text-sm font-semibold text-red-400 hover:underline disabled:opacity-50"
                >
                  {isDeletingAvatar
                    ? "Removing..."
                    : "Remove Avatar"}
                </button>
              )}
            </div>

            <input
              id="user-settings-avatar"
              type="file"
              accept=".jpg,.jpeg,.png,.webp"
              onChange={
                handleAvatarChange
              }
              className="hidden"
            />

            <p className="mt-1 text-xs text-gray-400">
              JPG, PNG or WEBP · Max 5 MB
            </p>
          </div>

          <label className="mt-6 block text-xs font-bold uppercase text-gray-300">
            Display name
          </label>

          <input
            value={displayName}
            onChange={event =>
              setDisplayName(
                event.target.value
              )
            }
            maxLength={100}
            className="mt-2 w-full rounded-md bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-primary"
          />

          <label className="mt-4 block text-xs font-bold uppercase text-gray-300">
            Username
          </label>

          <input
            value={
              currentUser.username ??
              ""
            }
            disabled
            className="mt-2 w-full cursor-not-allowed rounded-md bg-[#1e1f22] px-3 py-3 text-sm text-gray-500"
          />

          <p className="mt-1 text-xs text-gray-500">
            Username changes will be
            added separately.
          </p>

          <label className="mt-4 block text-xs font-bold uppercase text-gray-300">
            About me
          </label>

          <textarea
            value={bio}
            onChange={event =>
              setBio(
                event.target.value
              )
            }
            maxLength={500}
            rows={4}
            placeholder="Tell everyone a little about yourself."
            className="mt-2 w-full resize-none rounded-md bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-primary"
          />

          {error && (
            <p className="mt-4 rounded-md bg-red-500/10 px-3 py-2 text-sm text-red-400">
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
                !displayName.trim()
              }
              className="rounded-md bg-primary px-5 py-2.5 text-sm font-semibold hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isSaving
                ? "Saving..."
                : "Save Changes"}
            </button>
          </div>
        </form>
      </div>
    </div>,
    document.body
  );
}