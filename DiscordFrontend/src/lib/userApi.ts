import { API_URL, apiRequest } from "@/lib/api";
import type { AuthenticatedUser } from "@/lib/authApi";
import {
  StaticUserStatuses,
  type User,
  type UserStatuses
} from "@/lib/entities/user";

export interface UpdateProfileRequest {
  displayName: string;
  bio: string | null;
}

export interface ChangeStatusResponse {
  preferredStatus: string;
  visibleStatus: string;
}

export function getCurrentUser(
  accessToken: string
): Promise<AuthenticatedUser> {
  return apiRequest<AuthenticatedUser>(
    "/api/v1/users/me",
    {
      method: "GET"
    },
    accessToken
  );
}

export function updateCurrentUserProfile(
  request: UpdateProfileRequest,
  accessToken: string
): Promise<AuthenticatedUser> {
  return apiRequest<AuthenticatedUser>(
    "/api/v1/users/me",
    {
      method: "PATCH",
      body: JSON.stringify(request)
    },
    accessToken
  );
}

export function changeCurrentUserStatus(
  status: UserStatuses
): Promise<ChangeStatusResponse> {
  return apiRequest<ChangeStatusResponse>(
    "/api/v1/users/me/status",
    {
      method: "PATCH",
      body: JSON.stringify({
        status: mapUserStatusToApiValue(status)
      })
    }
  );
}

export function uploadCurrentUserAvatar(
  file: File,
  accessToken: string
): Promise<AuthenticatedUser> {
  const formData = new FormData();

  formData.append("file", file);

  return apiRequest<AuthenticatedUser>(
    "/api/v1/users/me/avatar",
    {
      method: "POST",
      body: formData
    },
    accessToken
  );
}

export function deleteCurrentUserAvatar(
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    "/api/v1/users/me/avatar",
    {
      method: "DELETE"
    },
    accessToken
  );
}
function mapUserStatusToApiValue(
  status: UserStatuses
): number {
  switch (status) {
    case StaticUserStatuses.Online:
      return 1;

    case StaticUserStatuses.Idle:
      return 2;

    case StaticUserStatuses.DND:
      return 3;

    default:
      return 4;
  }
}

function mapUserStatus(
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

function getAvatarUrl(
  avatarUrl: string | null
): string | null {
  if (!avatarUrl) {
    return null;
  }

  if (
    avatarUrl.startsWith("http://") ||
    avatarUrl.startsWith("https://")
  ) {
    return avatarUrl;
  }

  return `${API_URL}${avatarUrl}`;
}

export function mapAuthenticatedUser(
  user: AuthenticatedUser
): User {
  return {
    id: user.id.toString(),
    name: user.displayName,
    username: user.username,
    bio: user.bio ?? undefined,
    avatar: getAvatarUrl(user.avatarUrl),
    status: mapUserStatus(user.status),
    type: "user"
  };
}