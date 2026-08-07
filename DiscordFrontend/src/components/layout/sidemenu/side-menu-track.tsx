"use client";
import React, {
  useEffect,
  useState,
} from "react";
import { ListedServer } from "@/lib/entities/server";
import SideMenuItem from "./side-menu-item";
import { clsx } from "@/lib/utils";
import {
  BsDiscord,
  BsPlus,
} from "react-icons/bs";
import { TooltipProvider } from "@radix-ui/react-tooltip";
import Divider from "@/components/ui/divider";
import {
  getMyConversations,
} from "@/lib/conversationApi";

import {
  useAuthStore,
} from "@/state/auth";
type SideMenuTrackProps = {
  servers: ListedServer[];
  onOpenServerModal: () => void;
};

type ServerMenuItemProps = {
  server: ListedServer;
  isActive: boolean;
} & React.ComponentProps<typeof SideMenuItem>;

const ServerMenuItem = ({
  server,
  isActive,
  ...props
}: Omit<ServerMenuItemProps, "tooltipContent">) => {
  return (
    <SideMenuItem
      isActive={isActive}
      notificationCount={server.messages}
      tooltipContent={<div className="font-semibold">{server.name}</div>}
      className="mx-auto my-2"
      image={{
        url: server.photo,
        alt: server.name,
      }}
      {...props}
    />
  );
};

export default function SideMenuTrack({
  servers,
  onOpenServerModal,
}: SideMenuTrackProps) {
  const [active, setActive] = useState<string>("default");
const accessToken = useAuthStore(
  state => state.accessToken
);

const [
  dmUnreadCount,
  setDmUnreadCount,
] = useState(0);

useEffect(() => {
  if (!accessToken) {
    setDmUnreadCount(0);
    return;
  }

  let isCancelled = false;

  const loadDmUnreadCount = async () => {
    try {
      const conversations =
        await getMyConversations(
          accessToken
        );

      const totalUnread =
        conversations.reduce(
          (total, conversation) =>
            total +
            (conversation.unreadCount ?? 0),
          0
        );

      if (!isCancelled) {
        setDmUnreadCount(totalUnread);
      }
    } catch {
      if (!isCancelled) {
        setDmUnreadCount(0);
      }
    }
  };

  const handleUnreadChanged = () => {
    void loadDmUnreadCount();
  };

  void loadDmUnreadCount();

  window.addEventListener(
    "dm-unread:changed",
    handleUnreadChanged
  );

  return () => {
    isCancelled = true;

    window.removeEventListener(
      "dm-unread:changed",
      handleUnreadChanged
    );
  };
}, [accessToken]);
  return (
    <>
      <TooltipProvider>
        {/*
          Direct messages side menu button
        */}
        <SideMenuItem
          href="/channels/me"
          onClick={() => setActive("default")}
          tooltipContent={<div className="font-semibold">Direct messages</div>}
       notificationCount={dmUnreadCount}
          className={clsx(
            "mx-auto mb-2 flex items-center justify-center bg-foreground",
            active === "default" ? "bg-primary text-white" : "text-gray-300",
          )}
          isActive={active === "default"}
        >
          <BsDiscord fontSize={26} />
        </SideMenuItem>

        <Divider className="w-8" />

        {/*
          List of servers
        */}
        {servers?.map((server) => (
          <ServerMenuItem
            href={`/servers/${server.id}`}
            key={server.id}
            server={server}
            isActive={active === server.id}
            onClick={() => {
              setActive(server.id);
            }}
          />
        ))}
        <button
  type="button"
  title="Add a Server"
  aria-label="Add a Server"
  onClick={onOpenServerModal}
  className={clsx(
    "mx-auto my-2 flex h-12 w-12 items-center justify-center",
    "rounded-full bg-foreground text-green-500 transition-all",
    "hover:rounded-[15px] hover:bg-green-600 hover:text-white"
  )}
>
  <BsPlus fontSize={30} />
</button>
      </TooltipProvider>
    </>
  );
}
