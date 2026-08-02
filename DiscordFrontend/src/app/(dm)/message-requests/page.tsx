"use client";

import {
  useEffect,
  useState,
} from "react";

import { useRouter } from "next/navigation";

import {
  acceptMessageRequest,
  getIncomingMessageRequests,
  ignoreMessageRequest,
  markMessageRequestAsSpam,
} from "@/lib/messageRequestApi";

import type {
  DirectMessageRequestResponse,
} from "@/lib/messageRequestApi";

import { useAuthStore } from "@/state/auth";

export default function MessageRequestsPage() {
  const router = useRouter();

  const accessToken = useAuthStore(
    state => state.accessToken
  );

  const [messageRequests, setMessageRequests] =
    useState<DirectMessageRequestResponse[]>([]);

  const [isLoading, setIsLoading] =
    useState(true);

  const [
    processingRequestId,
    setProcessingRequestId,
  ] = useState<number | null>(null);

  const [error, setError] =
    useState<string | null>(null);

  const [actionError, setActionError] =
    useState<string | null>(null);

  useEffect(() => {
    if (!accessToken) {
      setMessageRequests([]);
      setIsLoading(false);
      return;
    }

    let isCancelled = false;

    const loadMessageRequests = async () => {
      try {
        setIsLoading(true);
        setError(null);

        const response =
          await getIncomingMessageRequests(
            accessToken
          );

        if (!isCancelled) {
          setMessageRequests(response);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : "Mesaj sorğuları yüklənə bilmədi."
          );
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    };

    void loadMessageRequests();

    return () => {
      isCancelled = true;
    };
  }, [accessToken]);

const removeRequestFromList = (
  requestId: number
) => {
  setMessageRequests(currentRequests =>
    currentRequests.filter(
      request =>
        request.id !== requestId
    )
  );

  window.dispatchEvent(
    new Event(
      "message-requests:refresh"
    )
  );
};

  const handleAccept = async (
    messageRequest: DirectMessageRequestResponse
  ) => {
    if (
      !accessToken ||
      processingRequestId !== null
    ) {
      return;
    }

    try {
      setProcessingRequestId(
        messageRequest.id
      );

      setActionError(null);

      await acceptMessageRequest(
        messageRequest.id,
        accessToken
      );

      removeRequestFromList(
        messageRequest.id
      );

      window.dispatchEvent(
        new Event("conversations:refresh")
      );

      router.push(
        `/channels/${messageRequest.conversationId}`
      );

      router.refresh();
    } catch (acceptError) {
      setActionError(
        acceptError instanceof Error
          ? acceptError.message
          : "Mesaj sorğusu qəbul edilə bilmədi."
      );
    } finally {
      setProcessingRequestId(null);
    }
  };

  const handleIgnore = async (
    requestId: number
  ) => {
    if (
      !accessToken ||
      processingRequestId !== null
    ) {
      return;
    }

    try {
      setProcessingRequestId(requestId);
      setActionError(null);

      await ignoreMessageRequest(
        requestId,
        accessToken
      );

      removeRequestFromList(requestId);
    } catch (ignoreError) {
      setActionError(
        ignoreError instanceof Error
          ? ignoreError.message
          : "Mesaj sorğusu silinə bilmədi."
      );
    } finally {
      setProcessingRequestId(null);
    }
  };

  const handleSpam = async (
    requestId: number
  ) => {
    if (
      !accessToken ||
      processingRequestId !== null
    ) {
      return;
    }

    const confirmed = window.confirm(
      "Bu mesaj sorğusunu spam kimi işarələmək istəyirsiniz?"
    );

    if (!confirmed) {
      return;
    }

    try {
      setProcessingRequestId(requestId);
      setActionError(null);

      await markMessageRequestAsSpam(
        requestId,
        accessToken
      );

      removeRequestFromList(requestId);
    } catch (spamError) {
      setActionError(
        spamError instanceof Error
          ? spamError.message
          : "Mesaj sorğusu spam kimi işarələnə bilmədi."
      );
    } finally {
      setProcessingRequestId(null);
    }
  };

  if (isLoading) {
    return (
      <main className="ml-[330px] flex min-h-screen items-center justify-center bg-background text-gray-300">
        Mesaj sorğuları yüklənir...
      </main>
    );
  }

  return (
    <main className="ml-[330px] min-h-screen bg-background text-white">
      <header className="border-b border-black/20 px-8 py-6 shadow">
        <h1 className="text-2xl font-semibold">
          Message Requests
        </h1>

        <p className="mt-1 text-sm text-gray-400">
          Tanımadığınız istifadəçilərdən gələn
          mesaj sorğularını burada idarə edə
          bilərsiniz.
        </p>
      </header>

      <section className="mx-auto max-w-4xl px-8 py-8">
        {error && (
          <div className="rounded-lg bg-red-950/40 px-5 py-4 text-red-300">
            {error}
          </div>
        )}

        {actionError && (
          <div className="mb-5 rounded-lg bg-red-950/40 px-5 py-4 text-red-300">
            {actionError}
          </div>
        )}

        {!error &&
          messageRequests.length === 0 && (
            <div className="flex min-h-[500px] flex-col items-center justify-center text-center">
              <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-white/5 text-3xl">
                📥
              </div>

              <h2 className="text-lg font-semibold">
                Mesaj sorğusu yoxdur
              </h2>

              <p className="mt-2 max-w-md text-sm text-gray-400">
                Yeni mesaj sorğusu gəldikdə
                burada görünəcək.
              </p>
            </div>
          )}

        {!error &&
          messageRequests.length > 0 && (
            <div className="space-y-4">
              {messageRequests.map(
                messageRequest => {
                  const senderInitial =
                    messageRequest
                      .senderDisplayName
                      .charAt(0)
                      .toUpperCase();

                  const isProcessing =
                    processingRequestId ===
                    messageRequest.id;

                  return (
                    <article
                      key={messageRequest.id}
                      className="rounded-xl border border-white/5 bg-white/[0.04] p-5 transition hover:bg-white/[0.06]"
                    >
                      <div className="flex items-start gap-4">
                        <div className="flex h-12 w-12 flex-none items-center justify-center rounded-full bg-primary text-lg font-semibold text-white">
                          {senderInitial}
                        </div>

                        <div className="min-w-0 flex-1">
                          <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
                            <h2 className="truncate font-semibold">
                              {
                                messageRequest.senderDisplayName
                              }
                            </h2>

                            <span className="text-sm text-gray-500">
                              @
                              {
                                messageRequest.senderUsername
                              }
                            </span>
                          </div>

                          <p className="mt-2 text-sm text-gray-400">
                            Sizə mesaj göndərmək
                            istəyir.
                          </p>

                          <time className="mt-2 block text-xs text-gray-500">
                            {new Date(
                              messageRequest.createdAt
                            ).toLocaleString(
                              "az-AZ"
                            )}
                          </time>
                        </div>
                      </div>

                      <div className="mt-5 flex flex-wrap justify-end gap-3">
                        <button
                          type="button"
                          disabled={
                            processingRequestId !==
                            null
                          }
                          onClick={() =>
                            void handleSpam(
                              messageRequest.id
                            )
                          }
                          className="rounded-md px-4 py-2 text-sm font-medium text-red-400 hover:bg-red-500/10 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          Spam
                        </button>

                        <button
                          type="button"
                          disabled={
                            processingRequestId !==
                            null
                          }
                          onClick={() =>
                            void handleIgnore(
                              messageRequest.id
                            )
                          }
                          className="rounded-md bg-white/10 px-4 py-2 text-sm font-medium text-gray-200 hover:bg-white/15 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          {isProcessing
                            ? "Gözləyin..."
                            : "Nəzərə alma"}
                        </button>

                        <button
                          type="button"
                          disabled={
                            processingRequestId !==
                            null
                          }
                          onClick={() =>
                            void handleAccept(
                              messageRequest
                            )
                          }
                          className="rounded-md bg-primary px-5 py-2 text-sm font-semibold text-white hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-50"
                        >
                          {isProcessing
                            ? "Gözləyin..."
                            : "Qəbul et"}
                        </button>
                      </div>
                    </article>
                  );
                }
              )}
            </div>
          )}
      </section>
    </main>
  );
}