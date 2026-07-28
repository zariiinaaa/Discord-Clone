"use client";

import { useEffect, useRef, useState } from "react";
import type { HubConnection } from "@microsoft/signalr";
import { createChatHubConnection } from "../../lib/chatHub";

export default function SignalRTestPage() {
  const connectionRef = useRef<HubConnection | null>(null);
  const joinedChannelIdRef = useRef<number | null>(null);

  const [accessToken, setAccessToken] = useState("");
  const [channelId, setChannelId] = useState("2");
  const [status, setStatus] = useState("Disconnected");
  const [logs, setLogs] = useState<string[]>([]);

  const addLog = (message: string) => {
    const time = new Date().toLocaleTimeString();

    setLogs(currentLogs => [
      `[${time}] ${message}`,
      ...currentLogs
    ]);
  };

  const connect = async () => {
    if (connectionRef.current) {
      addLog("SignalR artıq qoşulub.");
      return;
    }

    if (!accessToken.trim()) {
      addLog("Access token daxil edilməyib.");
      return;
    }

    const numericChannelId = Number(channelId);

    if (
      !Number.isInteger(numericChannelId) ||
      numericChannelId <= 0
    ) {
      addLog("Channel ID düzgün deyil.");
      return;
    }

    const connection =
      createChatHubConnection(accessToken.trim());

    connection.on("MessageCreated", message => {
      addLog(
        `MessageCreated: ${JSON.stringify(message)}`
      );
    });

    connection.on("MessageUpdated", message => {
      addLog(
        `MessageUpdated: ${JSON.stringify(message)}`
      );
    });

    connection.on(
      "MessageDeleted",
      (deletedChannelId, messageId) => {
        addLog(
          `MessageDeleted: channel=${deletedChannelId}, message=${messageId}`
        );
      }
    );

    connection.onreconnecting(() => {
      setStatus("Reconnecting...");
    });

    connection.onreconnected(() => {
      setStatus("Connected");
      addLog("SignalR yenidən qoşuldu.");
    });

    connection.onclose(() => {
      connectionRef.current = null;
      joinedChannelIdRef.current = null;
      setStatus("Disconnected");
    });

    try {
      setStatus("Connecting...");

      await connection.start();

      await connection.invoke(
        "JoinChannel",
        numericChannelId
      );

      connectionRef.current = connection;
      joinedChannelIdRef.current = numericChannelId;

      setStatus("Connected");

      addLog(
        `${numericChannelId} kanalına qoşululdu.`
      );
    } catch (error) {
      await connection.stop().catch(() => undefined);

      connectionRef.current = null;
      joinedChannelIdRef.current = null;

      setStatus("Connection failed");
      addLog(`Xəta: ${String(error)}`);
    }
  };

  const disconnect = async () => {
    const connection = connectionRef.current;

    if (!connection) {
      addLog("Aktiv SignalR bağlantısı yoxdur.");
      return;
    }

    try {
      if (joinedChannelIdRef.current !== null) {
        await connection.invoke(
          "LeaveChannel",
          joinedChannelIdRef.current
        );
      }

      await connection.stop();
    } finally {
      connectionRef.current = null;
      joinedChannelIdRef.current = null;

      setStatus("Disconnected");
      addLog("SignalR bağlantısı dayandırıldı.");
    }
  };

  useEffect(() => {
    return () => {
      connectionRef.current?.stop();
    };
  }, []);

  return (
    <main
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 999999,
        overflowY: "auto",
        padding: 32,
        fontFamily: "Arial, sans-serif",
        color: "#f2f3f5",
        backgroundColor: "#1e1f22"
      }}
    >
      <section
        style={{
          maxWidth: 900,
          margin: "0 auto",
          padding: 30,
          borderRadius: 12,
          backgroundColor: "#2b2d31"
        }}
      >
        <h1
          style={{
            marginBottom: 10,
            fontSize: 30,
            fontWeight: 700
          }}
        >
          SignalR Chat Test
        </h1>

        <p style={{ marginBottom: 24 }}>
          Status:{" "}
          <strong
            style={{
              color:
                status === "Connected"
                  ? "#23a559"
                  : "#f23f42"
            }}
          >
            {status}
          </strong>
        </p>

        <label
          style={{
            display: "block",
            marginBottom: 8,
            fontWeight: 600
          }}
        >
          Access token
        </label>

        <textarea
          value={accessToken}
          onChange={event =>
            setAccessToken(event.target.value)
          }
          rows={5}
          placeholder="Swagger-dən götürdüyün access token..."
          style={{
            display: "block",
            width: "100%",
            marginBottom: 20,
            padding: 12,
            resize: "vertical",
            border: "1px solid #1e1f22",
            borderRadius: 6,
            color: "#f2f3f5",
            backgroundColor: "#1e1f22"
          }}
        />

        <label
          style={{
            display: "block",
            marginBottom: 8,
            fontWeight: 600
          }}
        >
          Channel ID
        </label>

        <input
          value={channelId}
          onChange={event =>
            setChannelId(event.target.value)
          }
          style={{
            display: "block",
            width: 200,
            marginBottom: 20,
            padding: 10,
            border: "1px solid #1e1f22",
            borderRadius: 6,
            color: "#f2f3f5",
            backgroundColor: "#1e1f22"
          }}
        />

        <button
          type="button"
          onClick={connect}
          style={{
            padding: "10px 18px",
            border: 0,
            borderRadius: 6,
            color: "white",
            backgroundColor: "#5865f2",
            cursor: "pointer"
          }}
        >
          Connect
        </button>

        <button
          type="button"
          onClick={disconnect}
          style={{
            marginLeft: 10,
            padding: "10px 18px",
            border: 0,
            borderRadius: 6,
            color: "white",
            backgroundColor: "#da373c",
            cursor: "pointer"
          }}
        >
          Disconnect
        </button>

        <hr
          style={{
            margin: "28px 0",
            borderColor: "#3f4147"
          }}
        />

        <h2
          style={{
            marginBottom: 14,
            fontSize: 22,
            fontWeight: 700
          }}
        >
          Real-time events
        </h2>

        <div
          style={{
            minHeight: 180,
            padding: 16,
            borderRadius: 8,
            backgroundColor: "#1e1f22"
          }}
        >
          {logs.length === 0 ? (
            <p style={{ color: "#b5bac1" }}>
              Hələ real-time event yoxdur.
            </p>
          ) : (
            logs.map((log, index) => (
              <pre
                key={`${index}-${log}`}
                style={{
                  marginBottom: 10,
                  whiteSpace: "pre-wrap",
                  color: "#dbdee1"
                }}
              >
                {log}
              </pre>
            ))
          )}
        </div>
      </section>
    </main>
  );
}