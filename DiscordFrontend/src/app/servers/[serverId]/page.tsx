"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import {
  ChannelResponse,
  getServerById,
  ServerDetailsResponse,
} from "@/lib/serverApi";
import { useAuthStore } from "@/state/auth";

const CHANNEL_TYPE = {
  Text: 0,
  Voice: 1,
  Category: 2,
} as const;

function ChannelItem({
  channel,
  serverId,
}: {
  channel: ChannelResponse;
  serverId: number;
}) {
  const content = (
    <>
      <span className="w-5 text-center text-lg">
        {channel.type === CHANNEL_TYPE.Voice
          ? "🔊"
          : "#"}
      </span>

      <span className="truncate">
        {channel.name}
      </span>
    </>
  );

  const className =
    "flex w-full items-center gap-2 rounded px-2 py-1.5 text-left text-sm text-gray-400 hover:bg-white/5 hover:text-gray-200";

  if (channel.type === CHANNEL_TYPE.Text) {
    return (
      <Link
        href={`/servers/${serverId}/channels/${channel.id}`}
        className={className}
      >
        {content}
      </Link>
    );
  }

  return (
    <button
      type="button"
      className={className}
    >
      {content}
    </button>
  );
}

export default function ServerPage() {
  const params = useParams<{
    serverId: string;
  }>();

  const accessToken = useAuthStore(
    state => state.accessToken
  );

  const [server, setServer] =
    useState<ServerDetailsResponse | null>(null);

  const [isLoading, setIsLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    const serverId = Number(params.serverId);

    if (
      !accessToken ||
      !Number.isInteger(serverId) ||
      serverId <= 0
    ) {
      setError("Server məlumatı düzgün deyil.");
      setIsLoading(false);
      return;
    }

    let isCancelled = false;

    const loadServer = async () => {
      try {
        setIsLoading(true);
        setError(null);

        const response = await getServerById(
          serverId,
          accessToken
        );

        if (!isCancelled) {
          setServer(response);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : "Server yüklənə bilmədi."
          );
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    };

    loadServer();

    return () => {
      isCancelled = true;
    };
  }, [params.serverId, accessToken]);

  if (isLoading) {
    return (
      <main className="ml-[88px] flex min-h-screen items-center justify-center bg-background text-gray-300">
        Server yüklənir...
      </main>
    );
  }

  if (error || !server) {
    return (
      <main className="ml-[88px] flex min-h-screen items-center justify-center bg-background">
        <div className="rounded-lg bg-red-950/40 px-6 py-4 text-red-300">
          {error ?? "Server tapılmadı."}
        </div>
      </main>
    );
  }

  const categories = server.channels
    .filter(
      channel =>
        channel.type ===
        CHANNEL_TYPE.Category
    )
    .sort(
      (first, second) =>
        first.position - second.position
    );

  const uncategorizedChannels =
    server.channels
      .filter(
        channel =>
          channel.type !==
            CHANNEL_TYPE.Category &&
          channel.parentCategoryId === null
      )
      .sort(
        (first, second) =>
          first.position - second.position
      );

  return (
    <main className="ml-[88px] flex min-h-screen bg-background text-white">
      <aside className="flex w-60 flex-col bg-semibackground">
        <header className="border-b border-black/30 px-4 py-4 shadow">
          <h1 className="truncate font-semibold">
            {server.name}
          </h1>

          {server.description && (
            <p className="mt-1 truncate text-xs text-gray-400">
              {server.description}
            </p>
          )}
        </header>

        <div className="flex-1 overflow-y-auto px-2 py-3">
          {uncategorizedChannels.map(
            channel => (
              <ChannelItem
                key={channel.id}
                channel={channel}
                serverId={server.id}
              />
            )
          )}

          {categories.map(category => {
            const childChannels =
              server.channels
                .filter(
                  channel =>
                    channel.parentCategoryId ===
                    category.id
                )
                .sort(
                  (first, second) =>
                    first.position -
                    second.position
                );

            return (
              <section
                key={category.id}
                className="mt-4"
              >
                <h2 className="mb-1 px-2 text-[11px] font-bold uppercase tracking-wide text-gray-400">
                  {category.name}
                </h2>

                {childChannels.length > 0 ? (
                  childChannels.map(channel => (
                    <ChannelItem
                      key={channel.id}
                      channel={channel}
                      serverId={server.id}
                    />
                  ))
                ) : (
                  <p className="px-2 py-1 text-xs text-gray-500">
                    Kanal yoxdur
                  </p>
                )}
              </section>
            );
          })}
        </div>

        <footer className="border-t border-black/30 px-4 py-3 text-xs text-gray-400">
          👥 {server.memberCount} üzv
        </footer>
      </aside>

      <section className="flex flex-1 items-center justify-center">
        <div className="text-center">
          <h2 className="text-xl font-semibold">
            {server.name}
          </h2>

          <p className="mt-2 text-sm text-gray-400">
            Mesajları görmək üçün text kanal
            seçin.
          </p>
        </div>
      </section>
    </main>
  );
}