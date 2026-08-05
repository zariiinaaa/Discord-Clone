"use client";

import {
  type FormEvent,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import { useRouter } from "next/navigation";

import { Input } from "@/components/ui/input";
import InputField from "@/components/ui/input/input-field";
import { List } from "@/components/ui/list";
import {
  TooltipProvider,
} from "@/components/ui/tooltip";

import FriendListItem from "./friend-list-item";
import { EmptyBox } from "../empty-box-image";

import {
  FriendsTabEnum,
  friendsTabsProps,
} from "@/lib/types/friend-tab-prop";

import {
  StaticUserStatuses,
} from "@/lib/entities/user";

import type {
  User,
} from "@/lib/entities/user";

import {
  acceptFriendRequest,
  blockUser,
  cancelFriendRequest,
  getBlockedUsers,
  getFriends,
  getIncomingFriendRequests,
  getOutgoingFriendRequests,
  rejectFriendRequest,
  removeFriend,
  sendFriendRequest,
  unblockUser,
} from "@/lib/friendApi";

import type {
  FriendRequestResponse,
  FriendResponse,
  FriendUserResponse,
} from "@/lib/friendApi";

import {
  API_URL,
  apiRequest,
} from "@/lib/api";
import { normalizedCompare } from "@/lib/utils/string";
import clsx from "@/lib/clsx";

import { useAuthStore } from "@/state/auth";
import { useFriendsTabStore } from "@/state/friends-tab";
import { useFriendStore } from "@/state/friend-list";
import {
  useFriendRequestStore,
} from "@/state/friendRequest-list";

import {
  BsSearch,
  BsXLg,
} from "react-icons/bs";

type RequestDirection =
  | "incoming"
  | "outgoing";

interface FriendEntry {
  key: string;
  userId: number;
  user: User;
  requestId?: number;
  requestDirection?: RequestDirection;
}

interface DirectConversationResponse {
  id: number;
}

function mapStatus(
  status: number
): StaticUserStatuses {
  switch (status) {
    case 1:
      return StaticUserStatuses.Online;

    case 2:
      return StaticUserStatuses.Idle;

    case 3:
      return StaticUserStatuses.DND;

    default:
      return StaticUserStatuses.Offline;
  }
}

function mapUser(
  user: FriendUserResponse
): User {
  return {
    id: user.id.toString(),
    name: user.displayName,
    username: user.username,
   avatar: user.avatarUrl
  ? user.avatarUrl.startsWith("http://") ||
    user.avatarUrl.startsWith("https://")
    ? user.avatarUrl
    : `${API_URL}${
        user.avatarUrl.startsWith("/")
          ? ""
          : "/"
      }${user.avatarUrl}`
  : null,
    status: mapStatus(user.status),
  };
}

function getErrorMessage(
  error: unknown,
  fallback: string
): string {
  return error instanceof Error
    ? error.message
    : fallback;
}

export default function FriendList() {
  const router = useRouter();

  const accessToken = useAuthStore(
    state => state.accessToken
  );

  const currentTab =
    useFriendsTabStore(
      state => state.currentTab
    );

  const setStoredFriends =
    useFriendStore(
      state => state.setFriends
    );

  const setStoredFriendRequests =
    useFriendRequestStore(
      state =>
        state.setFriendRequests
    );

  const [friends, setFriends] =
    useState<FriendResponse[]>([]);

  const [
    incomingRequests,
    setIncomingRequests,
  ] = useState<
    FriendRequestResponse[]
  >([]);

  const [
    outgoingRequests,
    setOutgoingRequests,
  ] = useState<
    FriendRequestResponse[]
  >([]);

  const [
    blockedUsers,
    setBlockedUsers,
  ] = useState<
    FriendUserResponse[]
  >([]);

  const [search, setSearch] =
    useState("");

  const [username, setUsername] =
    useState("");

  const [isLoading, setIsLoading] =
    useState(true);

  const [actionKey, setActionKey] =
    useState<string | null>(null);

  const [error, setError] =
    useState<string | null>(null);

  const [
    actionMessage,
    setActionMessage,
  ] = useState<string | null>(null);

  const [
    actionError,
    setActionError,
  ] = useState<string | null>(null);

  const loadData = useCallback(
    async (
      showLoading: boolean
    ) => {
      if (!accessToken) {
        setFriends([]);
        setIncomingRequests([]);
        setOutgoingRequests([]);
        setBlockedUsers([]);

        setStoredFriends([]);
        setStoredFriendRequests([]);

        setError(
          "Please log in to view your friends."
        );

        setIsLoading(false);
        return;
      }

      try {
        if (showLoading) {
          setIsLoading(true);
        }

        setError(null);

        const [
          friendsResponse,
          incomingResponse,
          outgoingResponse,
          blockedResponse,
        ] = await Promise.all([
          getFriends(accessToken),

          getIncomingFriendRequests(
            accessToken
          ),

          getOutgoingFriendRequests(
            accessToken
          ),

          getBlockedUsers(accessToken),
        ]);

        setFriends(friendsResponse);

        setIncomingRequests(
          incomingResponse
        );

        setOutgoingRequests(
          outgoingResponse
        );

        setBlockedUsers(
          blockedResponse
        );

        setStoredFriends(
          friendsResponse.map(mapUser)
        );

        setStoredFriendRequests(
          incomingResponse.map(
            request =>
              mapUser(request.sender)
          )
        );
      } catch (loadError) {
        setError(
          getErrorMessage(
            loadError,
            "Friends could not be loaded."
          )
        );
      } finally {
        if (showLoading) {
          setIsLoading(false);
        }
      }
    },
    [
      accessToken,
      setStoredFriends,
      setStoredFriendRequests,
    ]
  );

  useEffect(() => {
    void loadData(true);
  }, [loadData]);

  useEffect(() => {
  if (!actionMessage) {
    return;
  }

  const timeoutId = window.setTimeout(() => {
    setActionMessage(null);
  }, 4000);

  return () => {
    window.clearTimeout(timeoutId);
  };
}, [actionMessage]);

useEffect(() => {
  if (!actionError) {
    return;
  }

  const timeoutId = window.setTimeout(() => {
    setActionError(null);
  }, 7000);

  return () => {
    window.clearTimeout(timeoutId);
  };
}, [actionError]);

  useEffect(() => {
 const handleFriendsRefresh = () => {
  setActionMessage(null);
  setActionError(null);

  void loadData(false);
};

  window.addEventListener(
    "friends:refresh",
    handleFriendsRefresh
  );

  return () => {
    window.removeEventListener(
      "friends:refresh",
      handleFriendsRefresh
    );
  };
}, [loadData]);

useEffect(() => {
  const handlePresenceChanged = (
    event: Event
  ) => {
    const presenceEvent =
      event as CustomEvent<{
        userId: number;
        status: number;
      }>;

    const { userId, status } =
      presenceEvent.detail;

setFriends(currentFriends =>
  currentFriends.map(friend =>
    friend.id === userId
      ? {
          ...friend,
          status,
        }
      : friend
  )
);

const storedFriends =
  useFriendStore.getState().friends ??
  [];

setStoredFriends(
  storedFriends.map(friend =>
    Number(friend.id) === userId
      ? {
          ...friend,
          status: mapStatus(status),
        }
      : friend
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
}, [setStoredFriends]);

  const executeAction =
    useCallback(
      async (
        key: string,
        action: () => Promise<unknown>,
        successMessage: string
      ): Promise<boolean> => {
        try {
          setActionKey(key);
          setActionError(null);
          setActionMessage(null);

          await action();
          await loadData(false);

          setActionMessage(
            successMessage
          );

          return true;
        } catch (actionFailure) {
          setActionError(
            getErrorMessage(
              actionFailure,
              "The operation could not be completed."
            )
          );

          return false;
        } finally {
          setActionKey(null);
        }
      },
      [loadData]
    );

  const handleSendRequest = async (
    event: FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault();

    const value = username.trim();

    if (
      !value ||
      !accessToken ||
      actionKey !== null
    ) {
      return;
    }

    const wasSuccessful =
      await executeAction(
        "send-request",
        () =>
          sendFriendRequest(
            {
              username: value,
            },
            accessToken
          ),
        `Friend request sent to @${value}.`
      );

    if (wasSuccessful) {
      setUsername("");
    }
  };

  const handleAcceptRequest = (
    requestId: number
  ) => {
    if (!accessToken) {
      return;
    }

    void executeAction(
      `accept:${requestId}`,
      () =>
        acceptFriendRequest(
          requestId,
          accessToken
        ),
      "Friend request accepted."
    );
  };

  const handleRejectRequest = (
    requestId: number
  ) => {
    if (!accessToken) {
      return;
    }

    void executeAction(
      `reject:${requestId}`,
      () =>
        rejectFriendRequest(
          requestId,
          accessToken
        ),
      "Friend request declined."
    );
  };

  const handleCancelRequest = (
    requestId: number
  ) => {
    if (!accessToken) {
      return;
    }

    void executeAction(
      `cancel:${requestId}`,
      () =>
        cancelFriendRequest(
          requestId,
          accessToken
        ),
      "Friend request cancelled."
    );
  };

  const handleRemoveFriend = (
    friendUserId: number,
    displayName: string
  ) => {
    if (!accessToken) {
      return;
    }

    const confirmed =
      window.confirm(
        `Remove ${displayName} from your friends?`
      );

    if (!confirmed) {
      return;
    }

    void executeAction(
      `remove:${friendUserId}`,
      () =>
        removeFriend(
          friendUserId,
          accessToken
        ),
      `${displayName} was removed from your friends.`
    );
  };

  const handleBlockUser = (
    userId: number,
    displayName: string
  ) => {
    if (!accessToken) {
      return;
    }

    const confirmed =
      window.confirm(
        `Block ${displayName}?`
      );

    if (!confirmed) {
      return;
    }

    void executeAction(
      `block:${userId}`,
      () =>
        blockUser(
          userId,
          accessToken
        ),
      `${displayName} was blocked.`
    );
  };

  const handleUnblockUser = (
    userId: number
  ) => {
    if (!accessToken) {
      return;
    }

    void executeAction(
      `unblock:${userId}`,
      () =>
        unblockUser(
          userId,
          accessToken
        ),
      "User was unblocked."
    );
  };

  const handleMessage = async (
    friendUserId: number
  ) => {
    if (!accessToken) {
      return;
    }

    try {
      setActionKey(
        `message:${friendUserId}`
      );

      setActionMessage(null);
      setActionError(null);

      const conversation =
        await apiRequest<
          DirectConversationResponse
        >(
          "/api/v1/conversations/direct",
          {
            method: "POST",
            body: JSON.stringify({
              otherUserId:
                friendUserId,
            }),
          },
          accessToken
        );

      router.push(
        `/channels/${conversation.id}`
      );
    } catch (messageError) {
      setActionError(
        getErrorMessage(
          messageError,
          "The conversation could not be opened."
        )
      );
    } finally {
      setActionKey(null);
    }
  };

  const entries =
    useMemo<FriendEntry[]>(
      () => {
        if (
          currentTab ===
            FriendsTabEnum.All ||
          currentTab ===
            FriendsTabEnum.Available
        ) {
          return friends.map(
            friend => ({
              key: `friend-${friend.id}`,
              userId: friend.id,
              user: mapUser(friend),
            })
          );
        }

        if (
          currentTab ===
          FriendsTabEnum.Pending
        ) {
          const incoming =
            incomingRequests.map(
              request => ({
                key:
                  `incoming-${request.id}`,
                userId:
                  request.sender.id,
                user: mapUser(
                  request.sender
                ),
                requestId:
                  request.id,
                requestDirection:
                  "incoming" as const,
              })
            );

          const outgoing =
            outgoingRequests.map(
              request => ({
                key:
                  `outgoing-${request.id}`,
                userId:
                  request.receiver.id,
                user: mapUser(
                  request.receiver
                ),
                requestId:
                  request.id,
                requestDirection:
                  "outgoing" as const,
              })
            );

          return [
            ...incoming,
            ...outgoing,
          ];
        }

        if (
          currentTab ===
          FriendsTabEnum.Blocked
        ) {
          return blockedUsers.map(
            user => ({
              key: `blocked-${user.id}`,
              userId: user.id,
              user: mapUser(user),
            })
          );
        }

        return [];
      },
      [
        currentTab,
        friends,
        incomingRequests,
        outgoingRequests,
        blockedUsers,
      ]
    );

  const tab =
    friendsTabsProps[currentTab];

  const filteredEntries =
    entries.filter(entry => {
      const matchesSearch =
        !search ||
        normalizedCompare(
          entry.user.name,
          search
        ) ||
        normalizedCompare(
          entry.user.username ?? "",
          search
        );

      const matchesStatus =
        tab.status
          ? tab.status.includes(
              entry.user.status
            )
          : true;

      return (
        matchesSearch &&
        matchesStatus
      );
    });

  if (isLoading) {
    return (
      <div className="flex flex-1 items-center justify-center text-sm text-gray-400">
        Loading friends...
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <div className="rounded-md bg-red-950/40 px-5 py-4 text-sm text-red-300">
          {error}
        </div>
      </div>
    );
  }

  if (
    currentTab ===
    FriendsTabEnum.AddFriend
  ) {
    return (
      <div className="flex flex-1 flex-col px-3 pt-2">
        <div className="border-b border-gray-800 pb-6">
          <h2 className="text-sm font-bold uppercase text-white">
            Add Friend
          </h2>

          <p className="mt-2 text-sm text-gray-400">
            You can add friends using
            their Discord Clone
            username.
          </p>

          <form
            onSubmit={handleSendRequest}
            className="mt-5 flex rounded-lg border border-gray-700 bg-gray-950/40 p-2 focus-within:border-primary"
          >
            <input
              type="text"
              value={username}
              onChange={event =>
                setUsername(
                  event.target.value
                )
              }
              placeholder="Enter a username"
              maxLength={50}
              autoComplete="off"
              className="min-w-0 flex-1 bg-transparent px-3 py-2 text-sm text-white outline-none placeholder:text-gray-500"
            />

            <button
              type="submit"
              disabled={
                actionKey !== null ||
                !username.trim()
              }
              className="rounded-md bg-primary px-4 py-2 text-sm font-semibold text-white hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {actionKey ===
              "send-request"
                ? "Sending..."
                : "Send Friend Request"}
            </button>
          </form>

          {actionMessage && (
            <p className="mt-3 text-sm text-green-400">
              {actionMessage}
            </p>
          )}

          {actionError && (
            <p className="mt-3 text-sm text-red-400">
              {actionError}
            </p>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      {(actionMessage ||
        actionError) && (
        <div className="px-2 pb-3">
          {actionMessage && (
            <p className="rounded-md bg-green-950/30 px-3 py-2 text-sm text-green-400">
              {actionMessage}
            </p>
          )}

          {actionError && (
            <p className="rounded-md bg-red-950/30 px-3 py-2 text-sm text-red-400">
              {actionError}
            </p>
          )}
        </div>
      )}

      {entries.length > 0 && (
        <div className="px-2 pb-5">
          <InputField
            endIcon={
              <>
                <BsSearch
                  className={clsx(
                    "absolute right-0 transition-all",
                    search
                      ? "-rotate-90 opacity-0"
                      : "rotate-0 opacity-100"
                  )}
                />

                <button
                  type="button"
                  aria-label="Clear search"
                  className={clsx(
                    "absolute right-0 outline-none transition-all",
                    search
                      ? "rotate-0 opacity-100"
                      : "rotate-90 opacity-0"
                  )}
                  onClick={() =>
                    setSearch("")
                  }
                >
                  <BsXLg />
                </button>
              </>
            }
          >
            <Input
              placeholder="Search"
              value={search}
              onChange={event =>
                setSearch(
                  event.target.value
                )
              }
            />
          </InputField>

          <div className="mt-6 text-xs font-semibold uppercase text-gray-400">
            {tab.title} —{" "}
            {filteredEntries.length}
          </div>
        </div>
      )}

      <div className="flex-1 overflow-y-auto">
        {filteredEntries.length > 0 ? (
          <TooltipProvider>
            <List>
              {filteredEntries.map(
                entry => {
                  const requestBusy =
                    entry.requestId !==
                      undefined &&
                    actionKey?.endsWith(
                      `:${entry.requestId}`
                    );

                  const userBusy =
                    actionKey?.endsWith(
                      `:${entry.userId}`
                    );

                  return (
                    <FriendListItem
                      key={entry.key}
                      friend={entry.user}
                      tab={tab}
                      requestDirection={
                        entry.requestDirection
                      }
                      isBusy={Boolean(
                        requestBusy ||
                          userBusy
                      )}
                      onAccept={
                        entry.requestId ===
                        undefined
                          ? undefined
                          : () =>
                              handleAcceptRequest(
                                entry.requestId!
                              )
                      }
                      onDecline={
                        entry.requestId ===
                        undefined
                          ? undefined
                          : () =>
                              handleRejectRequest(
                                entry.requestId!
                              )
                      }
                      onCancel={
                        entry.requestId ===
                        undefined
                          ? undefined
                          : () =>
                              handleCancelRequest(
                                entry.requestId!
                              )
                      }
                      onMessage={() =>
                        void handleMessage(
                          entry.userId
                        )
                      }
                      onRemove={() =>
                        handleRemoveFriend(
                          entry.userId,
                          entry.user.name
                        )
                      }
                      onBlock={() =>
                        handleBlockUser(
                          entry.userId,
                          entry.user.name
                        )
                      }
                      onUnblock={() =>
                        handleUnblockUser(
                          entry.userId
                        )
                      }
                    />
                  );
                }
              )}
            </List>
          </TooltipProvider>
        ) : (
          <EmptyBox
            src={tab.empty.imageSrc}
            alt={tab.empty.imageAlt}
            text={
              search
                ? "Whoops! No one was found with this name."
                : tab.empty.text
            }
          />
        )}
      </div>
    </div>
  );
}