import { apiRequest } from "@/lib/api";

export interface LoginRequest {
  emailOrUsername: string;
  password: string;
}

export interface AuthenticatedUser {
  id: number;
  username: string;
  displayName: string;
  email: string;
  avatarUrl: string | null;
  bio: string | null;
  status: number;
  role: number;
}

export interface AuthResponse {
  tokenType: string;
  accessToken: string;
  refreshToken: string;
  user: AuthenticatedUser;
}

export function login(
  request: LoginRequest
): Promise<AuthResponse> {
  return apiRequest<AuthResponse>(
    "/api/v1/auth/login",
    {
      method: "POST",
      body: JSON.stringify(request),
    }
  );
}

export function refreshSession(
  refreshToken: string
): Promise<AuthResponse> {
  return apiRequest<AuthResponse>(
    "/api/v1/auth/refresh",
    {
      method: "POST",
      body: JSON.stringify({
        refreshToken,
      }),
    }
  );
}