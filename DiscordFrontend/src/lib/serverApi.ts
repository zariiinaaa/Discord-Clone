import { apiRequest } from "@/lib/api";

export interface ServerResponse {
  id: number;
  name: string;
  description: string | null;
  iconUrl: string | null;
  bannerUrl: string | null;
  isPublic: boolean;
  ownerId: number;
  createdAt: string;
}

export interface ChannelResponse {
  id: number;
  name: string;
  topic: string | null;
  type: number;
  position: number;
  isPrivate: boolean;
  bitrate: number | null;
  userLimit: number | null;
  parentCategoryId: number | null;
}


export interface ServerMemberResponse {
  id: number;
  userId: number;
  username: string;
  displayName: string;
  avatarUrl: string | null;
  nickname: string | null;
  status: number;
  isOwner: boolean;
  isMuted: boolean;
  isDeafened: boolean;
  timedOutUntil: string | null;
  joinedAt: string;
}

export interface ServerDetailsResponse
  extends ServerResponse {
  memberCount: number;
  channels: ChannelResponse[];
}

export interface CreateServerRequest {
  name: string;
  description?: string | null;
  isPublic: boolean;
}

export interface UpdateServerRequest {
  name: string;
  description?: string | null;
  isPublic: boolean;
}

export interface CreateServerInviteRequest {
  expirationHours: number | null;
  maxUses: number | null;
}

export interface ServerInviteResponse {
  id: number;
  code: string;
  serverId: number;
  serverName: string;
  createdByUserId: number;
  expiresAt: string | null;
  maxUses: number | null;
  uses: number;
  isRevoked: boolean;
  createdAt: string;
}

export interface ServerInviteDetailsResponse {
  code: string;
  serverId: number;
  serverName: string;
  serverDescription: string | null;
  serverIconUrl: string | null;
  memberCount: number;
  createdByDisplayName: string;
  expiresAt: string | null;
  maxUses: number | null;
  uses: number;
  isAlreadyMember: boolean;
}

export function getMyServers(
  accessToken: string
): Promise<ServerResponse[]> {
  return apiRequest<ServerResponse[]>(
    "/api/v1/servers",
    {
      method: "GET",
    },
    accessToken
  );
}

export function getServerById(
  serverId: number,
  accessToken: string
): Promise<ServerDetailsResponse> {
  return apiRequest<ServerDetailsResponse>(
    `/api/v1/servers/${serverId}`,
    {
      method: "GET",
    },
    accessToken
  );
}

export function createServer(
  request: CreateServerRequest,
  accessToken: string
): Promise<ServerResponse> {
  return apiRequest<ServerResponse>(
    "/api/v1/servers",
    {
      method: "POST",
      body: JSON.stringify(request),
    },
    accessToken
  );
}

export function updateServer(
  serverId: number,
  request: UpdateServerRequest,
  accessToken: string
): Promise<ServerResponse> {
  return apiRequest<ServerResponse>(
    `/api/v1/servers/${serverId}`,
    {
      method: "PATCH",
      body: JSON.stringify(request),
    },
    accessToken
  );
}

export function deleteServer(
  serverId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/servers/${serverId}`,
    {
      method: "DELETE",
    },
    accessToken
  );
}

export function leaveServer(
  serverId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/servers/${serverId}/members/me`,
    {
      method: "DELETE",
    },
    accessToken
  );
}

export function createServerInvite(
  serverId: number,
  request: CreateServerInviteRequest,
  accessToken: string
): Promise<ServerInviteResponse> {
  return apiRequest<ServerInviteResponse>(
    `/api/v1/servers/${serverId}/invites`,
    {
      method: "POST",
      body: JSON.stringify(request),
    },
    accessToken
  );
}

export function getServerInvites(
  serverId: number,
  accessToken: string
): Promise<ServerInviteResponse[]> {
  return apiRequest<ServerInviteResponse[]>(
    `/api/v1/servers/${serverId}/invites`,
    {
      method: "GET",
    },
    accessToken
  );
}

export function revokeServerInvite(
  serverId: number,
  inviteId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/servers/${serverId}/invites/${inviteId}`,
    {
      method: "DELETE",
    },
    accessToken
  );
}

export function getServerInviteDetails(
  code: string,
  accessToken: string
): Promise<ServerInviteDetailsResponse> {
  return apiRequest<ServerInviteDetailsResponse>(
    `/api/v1/invites/${encodeURIComponent(code.trim())}`,
    {
      method: "GET",
    },
    accessToken
  );
}

export function joinServer(
  code: string,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/invites/${encodeURIComponent(code.trim())}/join`,
    {
      method: "POST",
    },
    accessToken
  );
}

export function uploadServerIcon(
  serverId: number,
  file: File,
  accessToken: string
): Promise<ServerResponse> {
  const formData = new FormData();

  formData.append(
    "icon",
    file
  );

  return apiRequest<ServerResponse>(
    `/api/v1/servers/${serverId}/icon`,
    {
      method: "PUT",
      body: formData,
    },
    accessToken
  );
}

export function getServerMembers(
  serverId: number,
  accessToken: string
): Promise<ServerMemberResponse[]> {
  return apiRequest<
    ServerMemberResponse[]
  >(
    `/api/v1/servers/${serverId}/members`,
    {
      method: "GET",
    },
    accessToken
  );
}
export function kickServerMember(
  serverId: number,
  memberUserId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/servers/${serverId}/members/${memberUserId}`,
    {
      method: "DELETE",
    },
    accessToken
  );
}