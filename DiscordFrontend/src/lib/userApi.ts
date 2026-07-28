import { API_URL, apiRequest } from "@/lib/api";
import type { AuthenticatedUser } from "@/lib/authApi";
import {
  StaticUserStatuses,
  type User
} from "@/lib/entities/user";

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