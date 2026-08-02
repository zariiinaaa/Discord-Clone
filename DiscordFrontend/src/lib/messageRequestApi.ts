import { apiRequest } from "@/lib/api";

export interface DirectMessageRequestResponse {
  id: number;
  conversationId: number;
  senderId: number;
  senderUsername: string;
  senderDisplayName: string;
  senderAvatarUrl: string | null;
  status: number;
  createdAt: string;
}

export function getIncomingMessageRequests(
  accessToken: string
): Promise<DirectMessageRequestResponse[]> {
  return apiRequest<
    DirectMessageRequestResponse[]
  >(
    "/api/v1/message-requests/incoming",
    {},
    accessToken
  );
}

export function acceptMessageRequest(
  requestId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/message-requests/${requestId}/accept`,
    {
      method: "POST",
    },
    accessToken
  );
}

export function ignoreMessageRequest(
  requestId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/message-requests/${requestId}/ignore`,
    {
      method: "POST",
    },
    accessToken
  );
}

export function markMessageRequestAsSpam(
  requestId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/message-requests/${requestId}/spam`,
    {
      method: "POST",
    },
    accessToken
  );
}