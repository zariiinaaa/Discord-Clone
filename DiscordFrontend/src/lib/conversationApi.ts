import { apiRequest } from "@/lib/api";

export interface ConversationMemberResponse {
  userId: number;
  username: string;
  displayName: string;
  avatarUrl: string | null;
  status: number;
  isMuted: boolean;
}

export interface ConversationResponse {
  id: number;
  type: number;
  name: string | null;
  iconUrl: string | null;
  ownerId: number | null;
  members: ConversationMemberResponse[];
  unreadCount: number;
  createdAt: string;
}

export interface ConversationMessageResponse {
  id: number;
  content: string;
  channelId: number | null;
  conversationId: number | null;
  authorId: number;
  authorUsername: string;
  authorDisplayName: string;
  authorAvatarUrl: string | null;
  replyToMessageId: number | null;
  isPinned: boolean;
  editedAt: string | null;
  createdAt: string;
}

export interface CreateConversationMessageRequest {
  content: string;
  replyToMessageId: number | null;
}

export interface UpdateConversationMessageRequest {
  content: string;
}

export function getConversationById(
  conversationId: number,
  accessToken: string
): Promise<ConversationResponse> {
  return apiRequest<ConversationResponse>(
    `/api/v1/conversations/${conversationId}`,
    {},
    accessToken
  );
}

export function getConversationMessages(
  conversationId: number,
  accessToken: string,
  beforeMessageId?: number,
  limit = 50
): Promise<ConversationMessageResponse[]> {
  const query = new URLSearchParams({
    limit: limit.toString()
  });

  if (beforeMessageId !== undefined) {
    query.set(
      "beforeMessageId",
      beforeMessageId.toString()
    );
  }

  return apiRequest<
    ConversationMessageResponse[]
  >(
    `/api/v1/conversations/${conversationId}/messages?${query.toString()}`,
    {},
    accessToken
  );
}

export function createConversationMessage(
  conversationId: number,
  request: CreateConversationMessageRequest,
  accessToken: string
): Promise<ConversationMessageResponse> {
  return apiRequest<
    ConversationMessageResponse
  >(
    `/api/v1/conversations/${conversationId}/messages`,
    {
      method: "POST",
      body: JSON.stringify(request)
    },
    accessToken
  );
}

export function updateConversationMessage(
  conversationId: number,
  messageId: number,
  request: UpdateConversationMessageRequest,
  accessToken: string
): Promise<ConversationMessageResponse> {
  return apiRequest<
    ConversationMessageResponse
  >(
    `/api/v1/conversations/${conversationId}/messages/${messageId}`,
    {
      method: "PATCH",
      body: JSON.stringify(request)
    },
    accessToken
  );
}

export function deleteConversationMessage(
  conversationId: number,
  messageId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/conversations/${conversationId}/messages/${messageId}`,
    {
      method: "DELETE"
    },
    accessToken
  );
}

export function getMyConversations(
  accessToken: string
): Promise<ConversationResponse[]> {
  return apiRequest<ConversationResponse[]>(
    "/api/v1/conversations",
    {},
    accessToken
  );
}

export function markConversationAsRead(
  conversationId: number,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/conversations/${conversationId}/read`,
    {
      method: "PUT"
    },
    accessToken
  );
}


export interface UpdateConversationMuteRequest {
  isMuted: boolean;
}

export function updateConversationMuteStatus(
  conversationId: number,
  request: UpdateConversationMuteRequest,
  accessToken: string
): Promise<void> {
  return apiRequest<void>(
    `/api/v1/conversations/${conversationId}/mute`,
    {
      method: "PUT",
      body: JSON.stringify(request),
    },
    accessToken
  );
}
