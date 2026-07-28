import { apiRequest } from "@/lib/api";

export interface MessageResponse {
  id: number;
  content: string;
  channelId: number;
  authorId: number;
  authorUsername: string;
  authorDisplayName: string;
  authorAvatarUrl: string | null;
  replyToMessageId: number | null;
  isPinned: boolean;
  editedAt: string | null;
  createdAt: string;
}

export interface CreateMessageRequest {
  content: string;
  replyToMessageId: number | null;
}

export interface UpdateMessageRequest {
  content: string;
}

export function getChannelMessages(
  channelId: number,
  accessToken: string,
  beforeMessageId?: number,
  limit = 50
): Promise<MessageResponse[]> {
  const searchParams =
    new URLSearchParams({
      limit: limit.toString(),
    });

  if (beforeMessageId) {
    searchParams.set(
      "beforeMessageId",
      beforeMessageId.toString()
    );
  }

  return apiRequest<MessageResponse[]>(
    `/api/v1/channels/${channelId}/messages?${searchParams.toString()}`,
    {
      method: "GET",
    },
    accessToken
  );
}

export function createMessage(
  channelId: number,
  request: CreateMessageRequest,
  accessToken: string
): Promise<MessageResponse> {
  return apiRequest<MessageResponse>(
    `/api/v1/channels/${channelId}/messages`,
    {
      method: "POST",
      body: JSON.stringify(request),
    },
    accessToken
  );
}

export function updateMessage(
  channelId: number,
  messageId: number,
  request: UpdateMessageRequest,
  accessToken: string
): Promise<MessageResponse> {
  return apiRequest<MessageResponse>(
    `/api/v1/channels/${channelId}/messages/${messageId}`,
    {
      method: "PATCH",
      body: JSON.stringify(request),
    },
    accessToken
  );
}

export function deleteMessage(
  channelId: number,
  messageId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/channels/${channelId}/messages/${messageId}`,
    {
      method: "DELETE",
    },
    accessToken
  );
}