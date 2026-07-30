"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";

import type { PropsWithChildren } from "react";

import {
  HubConnectionState,
} from "@microsoft/signalr";

import type {
  HubConnection,
} from "@microsoft/signalr";

import {
  createVoiceHubConnection,
} from "@/lib/voiceHub";

import type {
  VoiceParticipant,
} from "@/lib/voiceHub";

import { useAuthStore } from "@/state/auth";

type VoiceConnectionStatus =
  | "disconnected"
  | "connecting"
  | "connected"
  | "reconnecting";

interface VoiceContextValue {
  activeChannelId: number | null;
  participants: VoiceParticipant[];
  status: VoiceConnectionStatus;
  hasMicrophone: boolean;
  isMuted: boolean;

  joinVoiceChannel: (
    channelId: number
  ) => Promise<void>;

  leaveVoiceChannel: () => Promise<void>;

  toggleMute: () => void;
}

const VoiceContext =
  createContext<VoiceContextValue | null>(
    null
  );

function getMicrophoneErrorMessage(
  error: unknown
): string {
  if (error instanceof DOMException) {
    if (error.name === "NotAllowedError") {
      return "Mikrofon icazəsi verilmədi.";
    }

    if (error.name === "NotFoundError") {
      return "Kompüterdə mikrofon tapılmadı.";
    }

    if (error.name === "NotReadableError") {
      return "Mikrofon başqa proqram tərəfindən istifadə olunur.";
    }
  }

  return "Mikrofona qoşulmaq alınmadı.";
}

export function VoiceProvider({
  children,
}: PropsWithChildren) {
  const accessToken = useAuthStore(
    state => state.accessToken
  );

  const connectionRef =
    useRef<HubConnection | null>(null);

  const activeChannelIdRef =
    useRef<number | null>(null);

  const localStreamRef =
    useRef<MediaStream | null>(null);

  const peerConnectionsRef =
    useRef<
      Map<string, RTCPeerConnection>
    >(new Map());

  const remoteAudioElementsRef =
    useRef<
      Map<string, HTMLAudioElement>
    >(new Map());

  const pendingIceCandidatesRef =
    useRef<
      Map<string, RTCIceCandidateInit[]>
    >(new Map());

  const makingOfferRef =
    useRef<Set<string>>(new Set());

  const [activeChannelId, setActiveChannelId] =
    useState<number | null>(null);

  const [participants, setParticipants] =
    useState<VoiceParticipant[]>([]);

  const [status, setStatus] =
    useState<VoiceConnectionStatus>(
      "disconnected"
    );

  const [hasMicrophone, setHasMicrophone] =
    useState(false);

  const [isMuted, setIsMuted] =
    useState(false);

  const closePeerConnection =
    useCallback(
      (connectionId: string) => {
        const peerConnection =
          peerConnectionsRef.current.get(
            connectionId
          );

        if (peerConnection) {
          peerConnection.onicecandidate = null;
          peerConnection.ontrack = null;
          peerConnection.onconnectionstatechange =
            null;

          peerConnection.close();

          peerConnectionsRef.current.delete(
            connectionId
          );
        }

        const audioElement =
          remoteAudioElementsRef.current.get(
            connectionId
          );

        if (audioElement) {
          audioElement.pause();
          audioElement.srcObject = null;
          audioElement.remove();

          remoteAudioElementsRef.current.delete(
            connectionId
          );
        }

        pendingIceCandidatesRef.current.delete(
          connectionId
        );

        makingOfferRef.current.delete(
          connectionId
        );
      },
      []
    );

  const closeAllPeerConnections =
    useCallback(() => {
      const connectionIds = Array.from(
        peerConnectionsRef.current.keys()
      );

      connectionIds.forEach(
        closePeerConnection
      );

      pendingIceCandidatesRef.current.clear();
      makingOfferRef.current.clear();
    }, [closePeerConnection]);

  const stopMicrophone =
    useCallback(() => {
      const stream =
        localStreamRef.current;

      if (stream) {
        stream
          .getTracks()
          .forEach(track => track.stop());
      }

      localStreamRef.current = null;

      setHasMicrophone(false);
      setIsMuted(false);
    }, []);

  const ensureMicrophone =
    useCallback(async () => {
      const existingStream =
        localStreamRef.current;

      if (
        existingStream &&
        existingStream
          .getAudioTracks()
          .some(track =>
            track.readyState === "live"
          )
      ) {
        return existingStream;
      }

      if (
        !navigator.mediaDevices ||
        !navigator.mediaDevices.getUserMedia
      ) {
        throw new Error(
          "Brauzer mikrofon istifadəsini dəstəkləmir."
        );
      }

      try {
        const stream =
          await navigator.mediaDevices
            .getUserMedia({
              audio: {
                echoCancellation: true,
                noiseSuppression: true,
                autoGainControl: true,
              },
              video: false,
            });

        localStreamRef.current = stream;

        setHasMicrophone(true);
        setIsMuted(false);

        return stream;
      } catch (error) {
        throw new Error(
          getMicrophoneErrorMessage(error)
        );
      }
    }, []);

  const flushPendingIceCandidates =
    useCallback(
      async (
        targetConnectionId: string,
        peerConnection:
          RTCPeerConnection
      ) => {
        if (
          !peerConnection.remoteDescription
        ) {
          return;
        }

        const candidates =
          pendingIceCandidatesRef.current.get(
            targetConnectionId
          ) ?? [];

        for (const candidate of candidates) {
          await peerConnection.addIceCandidate(
            candidate
          );
        }

        pendingIceCandidatesRef.current.delete(
          targetConnectionId
        );
      },
      []
    );

  const getOrCreatePeerConnection =
    useCallback(
      (
        targetConnectionId: string
      ): RTCPeerConnection => {
        const existingPeerConnection =
          peerConnectionsRef.current.get(
            targetConnectionId
          );

        if (existingPeerConnection) {
          return existingPeerConnection;
        }

        const localStream =
          localStreamRef.current;

        if (!localStream) {
          throw new Error(
            "Mikrofon bağlantısı mövcud deyil."
          );
        }

        const peerConnection =
          new RTCPeerConnection({
            iceServers: [
              {
                urls:
                  "stun:stun.l.google.com:19302",
              },
            ],
          });

        peerConnectionsRef.current.set(
          targetConnectionId,
          peerConnection
        );

        localStream
          .getTracks()
          .forEach(track => {
            peerConnection.addTrack(
              track,
              localStream
            );
          });

        peerConnection.onicecandidate =
          event => {
            if (!event.candidate) {
              return;
            }

            const hubConnection =
              connectionRef.current;

            if (
              !hubConnection ||
              hubConnection.state !==
                HubConnectionState.Connected
            ) {
              return;
            }

            void hubConnection
              .invoke(
                "SendIceCandidate",
                targetConnectionId,
                JSON.stringify(
                  event.candidate.toJSON()
                )
              )
              .catch(error => {
                console.error(
                  "[Voice] ICE göndərilmədi:",
                  error
                );
              });
          };

        peerConnection.ontrack = event => {
          console.log(
            "[Voice] Remote audio track alındı:",
            targetConnectionId
          );

          const remoteStream =
            event.streams[0] ??
            new MediaStream([
              event.track,
            ]);

          let audioElement =
            remoteAudioElementsRef.current.get(
              targetConnectionId
            );

          if (!audioElement) {
            audioElement =
              document.createElement(
                "audio"
              );

            audioElement.autoplay = true;
audioElement.controls = false;
audioElement.volume = 1;
audioElement.muted = false;
audioElement.style.display = "none";

audioElement.setAttribute(
  "playsinline",
  "true"
);

            audioElement.setAttribute(
              "data-voice-connection",
              targetConnectionId
            );

            document.body.appendChild(
              audioElement
            );

            remoteAudioElementsRef.current.set(
              targetConnectionId,
              audioElement
            );
          }

          audioElement.srcObject =
            remoteStream;

          void audioElement
            .play()
            .then(() => {
              console.log(
                "[Voice] Remote audio başladıldı:",
                targetConnectionId
              );
            })
            .catch(error => {
              console.error(
                "[Voice] Remote audio başladılmadı:",
                error
              );
            });
        };

        peerConnection.onconnectionstatechange =
          () => {
            const connectionState =
              peerConnection.connectionState;

            console.log(
              `[Voice] Peer bağlantısı (${targetConnectionId}):`,
              connectionState
            );

            if (
              connectionState ===
                "failed" ||
              connectionState ===
                "closed"
            ) {
              closePeerConnection(
                targetConnectionId
              );
            }
          };

        return peerConnection;
      },
      [closePeerConnection]
    );

  const handleWebRtcOffer =
    useCallback(
      async (
        senderConnectionId: string,
        offerJson: string
      ) => {
        console.log(
          "[Voice] Offer alındı:",
          senderConnectionId
        );

        await ensureMicrophone();

        const peerConnection =
          getOrCreatePeerConnection(
            senderConnectionId
          );

        const offer =
          JSON.parse(
            offerJson
          ) as RTCSessionDescriptionInit;

        await peerConnection
          .setRemoteDescription(offer);

        await flushPendingIceCandidates(
          senderConnectionId,
          peerConnection
        );

        const answer =
          await peerConnection
            .createAnswer();

        await peerConnection
          .setLocalDescription(answer);

        const hubConnection =
          connectionRef.current;

        if (
          !hubConnection ||
          hubConnection.state !==
            HubConnectionState.Connected
        ) {
          throw new Error(
            "Voice hub bağlantısı mövcud deyil."
          );
        }

        await hubConnection.invoke(
          "SendWebRtcAnswer",
          senderConnectionId,
          JSON.stringify(answer)
        );
      },
      [
        ensureMicrophone,
        getOrCreatePeerConnection,
        flushPendingIceCandidates,
      ]
    );

  const handleWebRtcAnswer =
    useCallback(
      async (
        senderConnectionId: string,
        answerJson: string
      ) => {
        console.log(
          "[Voice] Answer alındı:",
          senderConnectionId
        );

        const peerConnection =
          peerConnectionsRef.current.get(
            senderConnectionId
          );

        if (!peerConnection) {
          console.warn(
            "[Voice] Answer üçün peer tapılmadı:",
            senderConnectionId
          );

          return;
        }

        const answer =
          JSON.parse(
            answerJson
          ) as RTCSessionDescriptionInit;

        await peerConnection
          .setRemoteDescription(answer);

        await flushPendingIceCandidates(
          senderConnectionId,
          peerConnection
        );
      },
      [flushPendingIceCandidates]
    );

  const handleIceCandidate =
    useCallback(
      async (
        senderConnectionId: string,
        candidateJson: string
      ) => {
        const candidate =
          JSON.parse(
            candidateJson
          ) as RTCIceCandidateInit;

        const peerConnection =
          peerConnectionsRef.current.get(
            senderConnectionId
          );

        if (
          !peerConnection ||
          !peerConnection.remoteDescription
        ) {
          const pendingCandidates =
            pendingIceCandidatesRef.current.get(
              senderConnectionId
            ) ?? [];

          pendingCandidates.push(
            candidate
          );

          pendingIceCandidatesRef.current.set(
            senderConnectionId,
            pendingCandidates
          );

          return;
        }

        await peerConnection
          .addIceCandidate(candidate);
      },
      []
    );

  const synchronizeParticipants =
    useCallback(
      async (
        updatedParticipants:
          VoiceParticipant[]
      ) => {
        const hubConnection =
          connectionRef.current;

        const ownConnectionId =
          hubConnection?.connectionId;

        if (
          !hubConnection ||
          !ownConnectionId ||
          hubConnection.state !==
            HubConnectionState.Connected
        ) {
          return;
        }

        console.log(
          "[Voice] Participants:",
          updatedParticipants
        );

        const remoteParticipants =
          updatedParticipants.filter(
            participant =>
              participant.connectionId !==
              ownConnectionId
          );

        const remoteConnectionIds =
          new Set(
            remoteParticipants.map(
              participant =>
                participant.connectionId
            )
          );

        Array.from(
          peerConnectionsRef.current.keys()
        ).forEach(connectionId => {
          if (
            !remoteConnectionIds.has(
              connectionId
            )
          ) {
            closePeerConnection(
              connectionId
            );
          }
        });

        for (
          const participant
          of remoteParticipants
        ) {
          const targetConnectionId =
            participant.connectionId;

          if (
            peerConnectionsRef.current.has(
              targetConnectionId
            ) ||
            makingOfferRef.current.has(
              targetConnectionId
            )
          ) {
            continue;
          }

          const shouldCreateOffer =
            ownConnectionId.localeCompare(
              targetConnectionId
            ) < 0;

          if (!shouldCreateOffer) {
            continue;
          }

          makingOfferRef.current.add(
            targetConnectionId
          );

          try {
            console.log(
              "[Voice] Offer yaradılır:",
              ownConnectionId,
              targetConnectionId
            );

            const peerConnection =
              getOrCreatePeerConnection(
                targetConnectionId
              );

            const offer =
              await peerConnection
                .createOffer();

            await peerConnection
              .setLocalDescription(offer);

            await hubConnection.invoke(
              "SendWebRtcOffer",
              targetConnectionId,
              JSON.stringify(offer)
            );
          } catch (error) {
            closePeerConnection(
              targetConnectionId
            );

            console.error(
              "[Voice] Offer yaradılmadı:",
              error
            );
          } finally {
            makingOfferRef.current.delete(
              targetConnectionId
            );
          }
        }
      },
      [
        closePeerConnection,
        getOrCreatePeerConnection,
      ]
    );

  const getConnection =
    useCallback(async () => {
      if (!accessToken) {
        throw new Error(
          "Voice kanalına qoşulmaq üçün hesaba daxil olun."
        );
      }

      let connection =
        connectionRef.current;

      if (!connection) {
        const newConnection =
          createVoiceHubConnection(
            accessToken
          );

        newConnection.on(
          "VoiceParticipantsUpdated",
          (
            channelId: number,
            updatedParticipants:
              VoiceParticipant[]
          ) => {
            if (
              activeChannelIdRef.current !==
              channelId
            ) {
              return;
            }

            setParticipants(
              updatedParticipants
            );

            void synchronizeParticipants(
              updatedParticipants
            ).catch(error => {
              console.error(
                "[Voice] İştirakçılar sinxronlaşdırılmadı:",
                error
              );
            });
          }
        );

        newConnection.on(
          "ReceiveWebRtcOffer",
          (
            senderConnectionId: string,
            offerJson: string
          ) => {
            void handleWebRtcOffer(
              senderConnectionId,
              offerJson
            ).catch(error => {
              console.error(
                "[Voice] Offer emal edilmədi:",
                error
              );
            });
          }
        );

        newConnection.on(
          "ReceiveWebRtcAnswer",
          (
            senderConnectionId: string,
            answerJson: string
          ) => {
            void handleWebRtcAnswer(
              senderConnectionId,
              answerJson
            ).catch(error => {
              console.error(
                "[Voice] Answer emal edilmədi:",
                error
              );
            });
          }
        );

        newConnection.on(
          "ReceiveIceCandidate",
          (
            senderConnectionId: string,
            candidateJson: string
          ) => {
            void handleIceCandidate(
              senderConnectionId,
              candidateJson
            ).catch(error => {
              console.error(
                "[Voice] ICE candidate emal edilmədi:",
                error
              );
            });
          }
        );

        newConnection.onreconnecting(
          () => {
            setStatus(
              "reconnecting"
            );
          }
        );

        newConnection.onreconnected(
          async () => {
            setStatus("connected");

            closeAllPeerConnections();

            const channelId =
              activeChannelIdRef.current;

            if (channelId === null) {
              return;
            }

            try {
              await ensureMicrophone();

              await newConnection.invoke(
                "JoinVoiceChannel",
                channelId
              );
            } catch (error) {
              console.error(
                "Voice kanalına yenidən qoşulmaq alınmadı:",
                error
              );

              activeChannelIdRef.current =
                null;

              setActiveChannelId(null);
              setParticipants([]);

              closeAllPeerConnections();
              stopMicrophone();
            }
          }
        );

        newConnection.onclose(() => {
          activeChannelIdRef.current =
            null;

          setActiveChannelId(null);
          setStatus("disconnected");
          setParticipants([]);

          closeAllPeerConnections();
          stopMicrophone();
        });

        connectionRef.current =
          newConnection;

        connection = newConnection;
      }

      if (
        connection.state ===
        HubConnectionState.Disconnected
      ) {
        try {
          setStatus("connecting");

          await connection.start();

          setStatus("connected");
        } catch (error) {
          connectionRef.current = null;

          setStatus("disconnected");

          throw error;
        }
      }

      return connection;
    }, [
      accessToken,
      synchronizeParticipants,
      handleWebRtcOffer,
      handleWebRtcAnswer,
      handleIceCandidate,
      closeAllPeerConnections,
      ensureMicrophone,
      stopMicrophone,
    ]);

  const joinVoiceChannel =
    useCallback(
      async (channelId: number) => {
        if (
          !Number.isInteger(channelId) ||
          channelId <= 0
        ) {
          throw new Error(
            "Voice kanal məlumatı düzgün deyil."
          );
        }

        const connection =
          await getConnection();

        await ensureMicrophone();

        const previousChannelId =
          activeChannelIdRef.current;

        if (
          previousChannelId === channelId
        ) {
          return;
        }

        if (
          previousChannelId !== null &&
          connection.state ===
            HubConnectionState.Connected
        ) {
          await connection.invoke(
            "LeaveVoiceChannel",
            previousChannelId
          );
        }

        closeAllPeerConnections();

        activeChannelIdRef.current =
          channelId;

        setActiveChannelId(channelId);
        setParticipants([]);

        try {
          await connection.invoke(
            "JoinVoiceChannel",
            channelId
          );
        } catch (error) {
          activeChannelIdRef.current =
            null;

          setActiveChannelId(null);
          setParticipants([]);

          closeAllPeerConnections();
          stopMicrophone();

          throw error;
        }
      },
      [
        getConnection,
        ensureMicrophone,
        closeAllPeerConnections,
        stopMicrophone,
      ]
    );

  const leaveVoiceChannel =
    useCallback(async () => {
      const connection =
        connectionRef.current;

      const channelId =
        activeChannelIdRef.current;

      try {
        if (
          connection &&
          channelId !== null &&
          connection.state ===
            HubConnectionState.Connected
        ) {
          await connection.invoke(
            "LeaveVoiceChannel",
            channelId
          );
        }
      } finally {
        activeChannelIdRef.current =
          null;

        setActiveChannelId(null);
        setParticipants([]);

        closeAllPeerConnections();
        stopMicrophone();
      }
    }, [
      closeAllPeerConnections,
      stopMicrophone,
    ]);

  const toggleMute =
    useCallback(() => {
      const stream =
        localStreamRef.current;

      if (!stream) {
        return;
      }

      setIsMuted(currentMuted => {
        const nextMuted =
          !currentMuted;

        stream
          .getAudioTracks()
          .forEach(track => {
            track.enabled =
              !nextMuted;
          });

        return nextMuted;
      });
    }, []);

  useEffect(() => {
    return () => {
      const connection =
        connectionRef.current;

      connectionRef.current = null;
      activeChannelIdRef.current =
        null;

      closeAllPeerConnections();
      stopMicrophone();

      if (connection) {
        void connection.stop();
      }
    };
  }, [
    closeAllPeerConnections,
    stopMicrophone,
  ]);

  const contextValue =
    useMemo<VoiceContextValue>(
      () => ({
        activeChannelId,
        participants,
        status,
        hasMicrophone,
        isMuted,
        joinVoiceChannel,
        leaveVoiceChannel,
        toggleMute,
      }),
      [
        activeChannelId,
        participants,
        status,
        hasMicrophone,
        isMuted,
        joinVoiceChannel,
        leaveVoiceChannel,
        toggleMute,
      ]
    );

  return (
    <VoiceContext.Provider
      value={contextValue}
    >
      {children}
    </VoiceContext.Provider>
  );
}

export function useVoice() {
  const context =
    useContext(VoiceContext);

  if (!context) {
    throw new Error(
      "useVoice yalnız VoiceProvider daxilində istifadə edilə bilər."
    );
  }

  return context;
}