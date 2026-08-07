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
  activeConversationId,
  status,
  hasMicrophone,
  canSpeak,
  isMuted,
  toggleMute,
  leaveVoiceChannel,
} = useVoice();

  const [isLeaving, setIsLeaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

 if (
  activeChannelId === null &&
  activeConversationId === null
) {
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

  const microphoneText = !canSpeak
    ? "Danışmaq icazəsi yoxdur"
    : hasMicrophone
      ? isMuted
        ? "Səssizdir"
        : "Aktivdir"
      : "Tapılmadı";

  const muteButtonTitle = !canSpeak
    ? "Bu kanalda Speak icazəniz yoxdur."
    : !hasMicrophone
      ? "Mikrofon tapılmadı."
      : isMuted
        ? "Mikrofonu aç"
        : "Mikrofonu söndür";

  return (
  <div className="fixed bottom-[64px] left-[88px] z-40 w-60 border-t border-black/30 bg-[#232428] px-3 py-3">
    <div className="flex items-center justify-between gap-3">
      <div className="min-w-0">
        <div className="flex items-center gap-2">
          <span
            className={`h-2.5 w-2.5 flex-none rounded-full ${
              status === "connected"
                ? "bg-green-500"
                : "bg-yellow-500"
            }`}
          />

          <p className="truncate text-sm font-semibold text-green-400">
            {activeConversationId !== null
              ? "DM zəngi bağlıdır"
              : "Voice bağlıdır"}
          </p>
        </div>

        <p className="mt-1 truncate text-xs text-gray-400">
          {microphoneText}
        </p>
      </div>

      <div className="flex flex-none items-center gap-2">
        <button
          type="button"
          onClick={toggleMute}
          disabled={!canSpeak || !hasMicrophone}
          title={muteButtonTitle}
          aria-pressed={isMuted}
          className={`flex h-9 w-9 items-center justify-center rounded-md transition ${
            isMuted
              ? "bg-red-500 text-white hover:bg-red-600"
              : "bg-white/10 text-gray-200 hover:bg-white/20"
          } disabled:cursor-not-allowed disabled:opacity-40`}
        >
          {isMuted ? "🎙️" : "🔇"}
        </button>

        <button
          type="button"
          onClick={handleLeave}
          disabled={isLeaving}
          title="Voice bağlantısından ayrıl"
          className="flex h-9 w-9 items-center justify-center rounded-md bg-red-500 text-white transition hover:bg-red-600 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isLeaving ? "..." : "📞"}
        </button>
      </div>
    </div>

    {error && (
      <p className="mt-2 text-xs text-red-400">
        {error}
      </p>
    )}
  </div>
);
}