import { useAuthStore } from "@/state/auth";

const API_URL = (
  process.env.NEXT_PUBLIC_API_URL ??
  "http://localhost:5151"
).replace(/\/$/, "");

interface ApiErrorResponse {
  title?: string;
  detail?: string;
  status?: number;
}

let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  const {
    refreshToken,
    setTokens,
    clearTokens,
  } = useAuthStore.getState();

  if (!refreshToken) {
    clearTokens();
    throw new Error("Refresh token tapılmadı.");
  }

  try {
    const { refreshSession } =
      await import("@/lib/authApi");

    const result = await refreshSession(
      refreshToken
    );

    setTokens(
      result.accessToken,
      result.refreshToken
    );

    return result.accessToken;
  } catch (error) {
    clearTokens();

    if (
      typeof window !== "undefined" &&
      window.location.pathname !== "/login"
    ) {
      window.location.href = "/login";
    }

    throw error;
  }
}

function getRefreshedAccessToken(): Promise<string> {
  if (!refreshPromise) {
    refreshPromise = refreshAccessToken()
      .finally(() => {
        refreshPromise = null;
      });
  }

  return refreshPromise;
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
  accessToken?: string,
  allowRefresh = true
): Promise<T> {
  const headers = new Headers(options.headers);

  const isAuthEndpoint =
    path.startsWith("/api/v1/auth/");

  if (
    options.body &&
    !(options.body instanceof FormData)
  ) {
    headers.set(
      "Content-Type",
      "application/json"
    );
  }

  const token =
    accessToken ??
    (!isAuthEndpoint
      ? useAuthStore.getState().accessToken
      : null);

  if (token) {
    headers.set(
      "Authorization",
      `Bearer ${token}`
    );
  }

  const response = await fetch(
    `${API_URL}${path}`,
    {
      ...options,
      headers,
    }
  );

  if (
    response.status === 401 &&
    allowRefresh &&
    !isAuthEndpoint
  ) {
    const newAccessToken =
      await getRefreshedAccessToken();

    return apiRequest<T>(
      path,
      options,
      newAccessToken,
      false
    );
  }

  if (!response.ok) {
    const errorResponse =
      (await response
        .json()
        .catch(() => null)) as
        ApiErrorResponse | null;

    throw new Error(
      errorResponse?.detail ??
        errorResponse?.title ??
        `API sorğusu uğursuz oldu: ${response.status}`
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export { API_URL };