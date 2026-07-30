"use client";

import { useState } from "react";
import { useVoice } from "./voice-provider";

function getStatusText(status: string): string {
  switch (status) {
    case "connecting":
      return "Qoşulur...";
    case "reconnecting":
      return "Yenidən qoşulur...";
    case "connected":
      return "Voice bağlıdır";
    default:
      return "Bağlantı yoxdur";
  }
}

export default function VoiceControlsPanel() {
  const {
    activeChannelId,
    status,
    hasMicrophone,
    isMuted,
    toggleMute,
    leaveVoiceChannel,
  } = useVoice();

  const [isLeaving, setIsLeaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  if (activeChannelId === null) {
    return null;
  }

  const handleLeave = async () => {
    if (isLeaving) {
      return;
    }

    try {
      setIsLeaving(true);
      setError(null);

      await leaveVoiceChannel();
    } catch (leaveError) {
      setError(
        leaveError instanceof Error
          ? leaveError.message
          : "Voice kanalından ayrılmaq alınmadı."
      );
    } finally {
      setIsLeaving(false);
    }
  };

  return (
    <div className="fixed bottom-0 left-[88px] z-50 w-60 border-t border-black/30 bg-[#232428] px-3 py-3 shadow-xl">
      <div className="mb-3">
        <div className="flex items-center gap-2">
          <span
            className={`h-2.5 w-2.5 rounded-full ${
              status === "connected"
                ? "bg-green-500"
                : "bg-yellow-500"
            }`}
          />

          <p className="text-sm font-semibold text-green-400">
            {getStatusText(status)}
          </p>
        </div>

        <p className="mt-1 text-xs text-gray-400">
          Voice kanal: {activeChannelId}
        </p>

        <p className="mt-1 text-xs text-gray-400">
          Mikrofon:{" "}
          {hasMicrophone
            ? isMuted
              ? "Səssizdir"
              : "Aktivdir"
            : "Tapılmadı"}
        </p>
      </div>

      {error && (
        <p className="mb-2 text-xs text-red-400">
          {error}
        </p>
      )}

      <div className="flex gap-2">
        <button
          type="button"
          onClick={toggleMute}
          disabled={!hasMicrophone}
          aria-pressed={isMuted}
          className={`flex-1 rounded-md px-3 py-2 text-xs font-semibold transition ${
            isMuted
              ? "bg-red-500 text-white hover:bg-red-600"
              : "bg-white/10 text-gray-200 hover:bg-white/20"
          } disabled:cursor-not-allowed disabled:opacity-40`}
        >
          {isMuted
            ? "🎙️ Səsi aç"
            : "🔇 Mute"}
        </button>

        <button
          type="button"
          onClick={handleLeave}
          disabled={isLeaving}
          title="Voice kanalından ayrıl"
          className="rounded-md bg-red-500 px-3 py-2 text-xs font-semibold text-white transition hover:bg-red-600 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isLeaving ? "..." : "📞"}
        </button>
      </div>
    </div>
  );
}