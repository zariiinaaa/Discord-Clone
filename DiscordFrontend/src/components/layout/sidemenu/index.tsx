"use client";

import { useEffect, useState } from "react";
import { ListedServer } from "@/lib/entities/server";
import { getMyServers } from "@/lib/serverApi";
import { API_URL } from "@/lib/api";
import { useAuthStore } from "@/state/auth";
import SideMenuTrack from "./side-menu-track";
import SideMenuWrapper from "./side-menu-wrapper";

function getServerPhoto(
  iconUrl: string | null
): string {
  if (!iconUrl) {
    return "/favicon.ico";
  }

  if (
    iconUrl.startsWith("http://") ||
    iconUrl.startsWith("https://")
  ) {
    return iconUrl;
  }

  return `${API_URL}${
    iconUrl.startsWith("/") ? "" : "/"
  }${iconUrl}`;
}

export default function SideMenu() {
  const accessToken = useAuthStore(
    state => state.accessToken
  );

  const [servers, setServers] =
    useState<ListedServer[]>([]);

  useEffect(() => {
    if (!accessToken) {
      setServers([]);
      return;
    }

    let isCancelled = false;

    const loadServers = async () => {
      try {
        const response =
          await getMyServers(accessToken);

        if (isCancelled) {
          return;
        }

        const mappedServers: ListedServer[] =
          response.map(server => ({
            id: server.id.toString(),
            name: server.name,
            photo: getServerPhoto(
              server.iconUrl
            ),
          }));

        setServers(mappedServers);
      } catch (error) {
        console.error(
          "Serverlər yüklənmədi:",
          error
        );
      }
    };

    loadServers();

    return () => {
      isCancelled = true;
    };
  }, [accessToken]);

  return (
    <SideMenuWrapper>
      <SideMenuTrack servers={servers} />
    </SideMenuWrapper>
  );
}