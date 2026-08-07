import React from "react";
import VoiceStatusButton from "./voice-status-button";
import {
  BsGearFill,
  BsHeadphones,
  BsMicFill,
  BsTelephoneXFill,
} from "react-icons/bs";
import { useVoice } from "@/components/voice/voice-provider";

interface VoiceControlsProps {
  voiceStatus: {
    mute?: boolean;
    deaf?: boolean;
  };
  setVoiceStatus: (
    statusUpdater: (prev: { mute?: boolean; deaf?: boolean }) => {
      mute?: boolean;
      deaf?: boolean;
    }
  ) => void;
  onOpenSettings: () => void;
}

function VoiceControls({ voiceStatus, setVoiceStatus, onOpenSettings }: VoiceControlsProps) {
  const {
  activeChannelId,
  activeConversationId,
  hasMicrophone,
  canSpeak,
  isMuted,
  toggleMute,
  leaveVoiceChannel,
} = useVoice();

  const isVoiceConnected =
    activeChannelId !== null ||
    activeConversationId !== null;

  const microphoneMuted = isVoiceConnected
    ? isMuted || voiceStatus.deaf
    : voiceStatus.mute || voiceStatus.deaf;

  const handleToggleMicrophone = () => {
    if (isVoiceConnected) {
      if (!canSpeak || !hasMicrophone) {
        return;
      }

      toggleMute();

      setVoiceStatus(prev => ({
        ...prev,
        deaf: false,
        mute: !isMuted,
      }));

      return;
    }

    setVoiceStatus(prev => ({
      ...prev,
      deaf: false,
      mute: !prev.mute,
    }));
  };

  return (
    <div className="flex items-center">
      <VoiceStatusButton
        muted={microphoneMuted}
        tooltipText={microphoneMuted ? "Unmute" : "Mute"}
        onClick={handleToggleMicrophone}
        icon={<BsMicFill fontSize={18} />}
      />

      <VoiceStatusButton
        muted={voiceStatus.deaf}
        tooltipText={voiceStatus.deaf ? "Undeaf" : "Deaf"}
        onClick={() =>
          setVoiceStatus(prev => ({
            ...prev,
            deaf: !prev.deaf,
          }))
        }
        icon={<BsHeadphones fontSize={20} />}
      />
{isVoiceConnected && (
  <VoiceStatusButton
    muted
    tooltipText="Disconnect"
    onClick={() => void leaveVoiceChannel()}
    icon={<BsTelephoneXFill fontSize={18} />}
  />
)}

      <VoiceStatusButton
        tooltipText="Settings"
        onClick={onOpenSettings}
        icon={<BsGearFill fontSize={18} />}
      />
    </div>
  );
}

export default VoiceControls;