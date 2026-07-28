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

export interface ServerDetailsResponse
  extends ServerResponse {
  memberCount: number;
  channels: ChannelResponse[];
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