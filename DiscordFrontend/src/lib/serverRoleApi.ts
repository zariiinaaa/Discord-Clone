import { apiRequest } from "@/lib/api";

export interface ServerRoleResponse {
  id: number;
  name: string;
  colorHex: string | null;
  position: number;
  isDefault: boolean;
  isDisplayedSeparately: boolean;
  isMentionable: boolean;
  serverId: number;
  permissions: number[];
}

export interface CreateServerRoleRequest {
  name: string;
  colorHex: string | null;
  isDisplayedSeparately: boolean;
  isMentionable: boolean;
  permissions: number[];
}

export interface UpdateServerRoleRequest {
  name: string;
  colorHex: string | null;
  isDisplayedSeparately: boolean;
  isMentionable: boolean;
  permissions: number[];
}

export function getServerRoles(
  serverId: number,
  accessToken: string
): Promise<ServerRoleResponse[]> {
  return apiRequest<ServerRoleResponse[]>(
    `/api/v1/servers/${serverId}/roles`,
    {},
    accessToken
  );
}

export function createServerRole(
  serverId: number,
  request: CreateServerRoleRequest,
  accessToken: string
): Promise<ServerRoleResponse> {
  return apiRequest<ServerRoleResponse>(
    `/api/v1/servers/${serverId}/roles`,
    {
      method: "POST",
      body: JSON.stringify(request),
    },
    accessToken
  );
}

export function updateServerRole(
  serverId: number,
  roleId: number,
  request: UpdateServerRoleRequest,
  accessToken: string
): Promise<ServerRoleResponse> {
  return apiRequest<ServerRoleResponse>(
    `/api/v1/servers/${serverId}/roles/${roleId}`,
    {
      method: "PATCH",
      body: JSON.stringify(request),
    },
    accessToken
  );
}

export function deleteServerRole(
  serverId: number,
  roleId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/servers/${serverId}/roles/${roleId}`,
    {
      method: "DELETE",
    },
    accessToken
  );
}

export function assignServerRole(
  serverId: number,
  roleId: number,
  memberUserId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/servers/${serverId}/roles/${roleId}/members/${memberUserId}`,
    {
      method: "PUT",
    },
    accessToken
  );
}

export function removeServerRole(
  serverId: number,
  roleId: number,
  memberUserId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/servers/${serverId}/roles/${roleId}/members/${memberUserId}`,
    {
      method: "DELETE",
    },
    accessToken
  );
}