"use client";

import { useState } from "react";

import Avatar from "@/components/ui/avatar";
import RoundedButton from "@/components/ui/button/rounded-button";

import type { User } from "@/lib/entities/user";
import type { FriendsTab } from "@/lib/types/friend-tab-prop";

import {
  FriendsTabEnum,
} from "@/lib/types/friend-tab-prop";

import { t } from "@/lib/i18n";

import {
  BsChatLeftFill,
  BsCheck2,
  BsThreeDotsVertical,
  BsX,
} from "react-icons/bs";

type FriendRequestDirection =
  | "incoming"
  | "outgoing";

interface FriendListItemProps {
  friend: User;
  tab: FriendsTab;

  requestDirection?: FriendRequestDirection;
  isBusy?: boolean;

  onAccept?: () => void;
  onDecline?: () => void;
  onCancel?: () => void;

  onMessage?: () => void;
  onRemove?: () => void;
  onBlock?: () => void;
  onUnblock?: () => void;
}

export default function FriendListItem({
  friend,
  tab,
  requestDirection = "incoming",
  isBusy = false,
  onAccept,
  onDecline,
  onCancel,
  onMessage,
  onRemove,
  onBlock,
  onUnblock,
}: FriendListItemProps) {
  const [isMenuOpen, setIsMenuOpen] =
    useState(false);

  const isPending =
    tab.key === FriendsTabEnum.Pending;

  const isBlocked =
    tab.key === FriendsTabEnum.Blocked;

  const subtitle = (() => {
    if (isPending) {
      return requestDirection === "incoming"
        ? "Incoming Friend Request"
        : "Outgoing Friend Request";
    }

    if (isBlocked) {
      return "Blocked";
    }

    return t(`user.status.${friend.status}`);
  })();

  const runMenuAction = (
    action?: () => void
  ) => {
    setIsMenuOpen(false);
    action?.();
  };

  return (
    <li className="group relative flex items-center justify-between border-t border-gray-800 py-2.5 pr-3">
      <div className="flex min-w-0 items-center gap-3">
        <Avatar
          src={friend.avatar}
          alt={friend.name}
          className="flex-none"
          status={
            isPending || isBlocked
              ? undefined
              : friend.status
          }
        />

        <div className="min-w-0 flex-1 leading-4">
          <div className="flex items-center gap-1.5 text-sm text-gray-200">
            <span className="truncate font-semibold">
              {friend.name}
            </span>

            {friend.username && (
              <span className="hidden truncate text-xs text-gray-400 group-hover:block">
                @{friend.username}
              </span>
            )}
          </div>

          <div className="mt-0.5 text-[13px] text-gray-300">
            {subtitle}
          </div>
        </div>
      </div>

      <div className="ml-3 flex flex-none items-center gap-2.5">
        {isPending &&
          requestDirection === "incoming" && (
            <>
              <RoundedButton
                className="!p-1.5"
                onClick={() => onAccept?.()}
                tooltipContent="Accept"
                disabled={isBusy}
              >
                <BsCheck2 size={23} />
              </RoundedButton>

              <RoundedButton
                className="!p-1.5"
                onClick={() => onDecline?.()}
                tooltipContent="Decline"
                disabled={isBusy}
              >
                <BsX size={23} />
              </RoundedButton>
            </>
          )}

        {isPending &&
          requestDirection === "outgoing" && (
            <RoundedButton
              className="!p-1.5"
              onClick={() => onCancel?.()}
              tooltipContent="Cancel Request"
              disabled={isBusy}
            >
              <BsX size={23} />
            </RoundedButton>
          )}

        {isBlocked && (
          <button
            type="button"
            disabled={isBusy}
            onClick={() => onUnblock?.()}
            className="rounded-md bg-gray-700 px-3 py-2 text-xs font-semibold text-gray-100 hover:bg-gray-600 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Unblock
          </button>
        )}

        {!isPending && !isBlocked && (
          <>
            <RoundedButton
              onClick={() => onMessage?.()}
              tooltipContent="Message"
              disabled={isBusy}
            >
              <BsChatLeftFill />
            </RoundedButton>

            <div className="relative">
              <RoundedButton
                onClick={() =>
                  setIsMenuOpen(
                    currentValue =>
                      !currentValue
                  )
                }
                tooltipContent="More"
                disabled={isBusy}
              >
                <BsThreeDotsVertical />
              </RoundedButton>

              {isMenuOpen && (
                <div className="absolute right-0 top-11 z-30 w-40 rounded-md bg-gray-950 p-1.5 shadow-xl ring-1 ring-black/40">
                  <button
                    type="button"
                    onClick={() =>
                      runMenuAction(onRemove)
                    }
                    className="w-full rounded px-3 py-2 text-left text-sm text-gray-200 hover:bg-primary hover:text-white"
                  >
                    Remove Friend
                  </button>

                  <button
                    type="button"
                    onClick={() =>
                      runMenuAction(onBlock)
                    }
                    className="w-full rounded px-3 py-2 text-left text-sm text-red-400 hover:bg-red-600 hover:text-white"
                  >
                    Block
                  </button>
                </div>
              )}
            </div>
          </>
        )}
      </div>
    </li>
  );
}