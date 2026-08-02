import { apiRequest } from "@/lib/api";

export const FRIEND_REQUEST_STATUS = {
  Pending: 0,
  Accepted: 1,
  Rejected: 2,
  Cancelled: 3,
} as const;

export type FriendRequestStatus =
  (typeof FRIEND_REQUEST_STATUS)[
    keyof typeof FRIEND_REQUEST_STATUS
  ];

export interface FriendUserResponse {
  id: number;
  username: string;
  displayName: string;
  avatarUrl: string | null;
  status: number;
}

export interface FriendResponse
  extends FriendUserResponse {
  friendsSince: string;
}

export interface FriendRequestResponse {
  id: number;
  sender: FriendUserResponse;
  receiver: FriendUserResponse;
  status: FriendRequestStatus;
  createdAt: string;
  respondedAt: string | null;
}

export interface SendFriendRequestRequest {
  username: string;
}

export function getFriends(
  accessToken: string
): Promise<FriendResponse[]> {
  return apiRequest<FriendResponse[]>(
    "/api/v1/friends",
    {},
    accessToken
  );
}

export function getIncomingFriendRequests(
  accessToken: string
): Promise<FriendRequestResponse[]> {
  return apiRequest<FriendRequestResponse[]>(
    "/api/v1/friends/requests/incoming",
    {},
    accessToken
  );
}

export function getOutgoingFriendRequests(
  accessToken: string
): Promise<FriendRequestResponse[]> {
  return apiRequest<FriendRequestResponse[]>(
    "/api/v1/friends/requests/outgoing",
    {},
    accessToken
  );
}

export function sendFriendRequest(
  request: SendFriendRequestRequest,
  accessToken: string
): Promise<FriendRequestResponse> {
  return apiRequest<FriendRequestResponse>(
    "/api/v1/friends/requests",
    {
      method: "POST",
      body: JSON.stringify(request),
    },
    accessToken
  );
}

export function acceptFriendRequest(
  requestId: number,
  accessToken: string
): Promise<FriendResponse> {
  return apiRequest<FriendResponse>(
    `/api/v1/friends/requests/${requestId}/accept`,
    {
      method: "POST",
    },
    accessToken
  );
}

export function rejectFriendRequest(
  requestId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/friends/requests/${requestId}/reject`,
    {
      method: "POST",
    },
    accessToken
  );
}

export function cancelFriendRequest(
  requestId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/friends/requests/${requestId}`,
    {
      method: "DELETE",
    },
    accessToken
  );
}

export function removeFriend(
  friendUserId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/friends/${friendUserId}`,
    {
      method: "DELETE",
    },
    accessToken
  );
}

export function getBlockedUsers(
  accessToken: string
): Promise<FriendUserResponse[]> {
  return apiRequest<FriendUserResponse[]>(
    "/api/v1/friends/blocked",
    {},
    accessToken
  );
}

export function blockUser(
  blockedUserId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/friends/blocked/${blockedUserId}`,
    {
      method: "POST",
    },
    accessToken
  );
}

export function unblockUser(
  blockedUserId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/friends/blocked/${blockedUserId}`,
    {
      method: "DELETE",
    },
    accessToken
  );
}