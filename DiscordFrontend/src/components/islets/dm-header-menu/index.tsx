"use client";

import {
  useCallback,
  useEffect,
  useState,
} from "react";

import {
  BsInboxFill,
  BsPersonFill,
  BsStars,
} from "react-icons/bs";

import { usePathname } from "next/navigation";

import {
  getIncomingFriendRequests,
} from "@/lib/friendApi";

import {
  List,
  ListItem,
} from "@/components/ui/list";

import Badge from "@/components/ui/badge";

import {
  getIncomingMessageRequests,
} from "@/lib/messageRequestApi";

import {
  useChatHub,
} from "@/components/chat/chat-hub-provider";

import { clsx } from "@/lib/utils";
import { useAuthStore } from "@/state/auth";



type HeaderMenuListItemProps = {
  icon: React.ReactNode;
  name: React.ReactNode;
  rightContent?: React.ReactNode;
  children?: never;
} & React.ComponentProps<typeof ListItem>;

const HeaderMenuListItem = ({
  icon,
  name,
  rightContent,
  className,
  ...props
}: HeaderMenuListItemProps) => (
  <ListItem
    noVerticalPadding
    className={clsx(
      "my-0.5 justify-between py-2.5",
      className
    )}
    {...props}
  >
    <span className="inline-flex items-center gap-3">
      {icon}
      {name}
    </span>

    {rightContent}
  </ListItem>
);

export default function DMHeaderMenu() {
  const pathname = usePathname();

  const accessToken = useAuthStore(
    state => state.accessToken
  );

 

  const { connection } = useChatHub();

  const [
  friendRequestCount,
  setFriendRequestCount,
] = useState(0);
  const [
    messageRequestCount,
    setMessageRequestCount,
  ] = useState(0);

  const loadFriendRequestCount =
  useCallback(async () => {
    if (!accessToken) {
      setFriendRequestCount(0);
      return;
    }

    try {
      const requests =
        await getIncomingFriendRequests(
          accessToken
        );

      setFriendRequestCount(
        requests.length
      );
    } catch (loadError) {
      console.error(
        "Friend request count could not be loaded:",
        loadError
      );
    }
  }, [accessToken]);




  const loadMessageRequestCount =
    useCallback(async () => {
      if (!accessToken) {
        setMessageRequestCount(0);
        return;
      }

      try {
        const requests =
          await getIncomingMessageRequests(
            accessToken
          );

        setMessageRequestCount(
          requests.length
        );
      } catch (loadError) {
        console.error(
          "Message request count could not be loaded:",
          loadError
        );
      }
    }, [accessToken]);


    useEffect(() => {
  void loadFriendRequestCount();

  const handleFriendsRefresh = () => {
    void loadFriendRequestCount();
  };

  window.addEventListener(
    "friends:refresh",
    handleFriendsRefresh
  );

  return () => {
    window.removeEventListener(
      "friends:refresh",
      handleFriendsRefresh
    );
  };
}, [loadFriendRequestCount]);
  useEffect(() => {
    void loadMessageRequestCount();

    const handleRequestsRefresh = () => {
      void loadMessageRequestCount();
    };

    window.addEventListener(
      "message-requests:refresh",
      handleRequestsRefresh
    );

    return () => {
      window.removeEventListener(
        "message-requests:refresh",
        handleRequestsRefresh
      );
    };
  }, [loadMessageRequestCount]);

  useEffect(() => {
    if (!connection) {
      return;
    }

    const handleRequestCreated = () => {
      void loadMessageRequestCount();
    };

    connection.on(
      "DirectMessageRequestCreated",
      handleRequestCreated
    );

    return () => {
      connection.off(
        "DirectMessageRequestCreated",
        handleRequestCreated
      );
    };
  }, [
    connection,
    loadMessageRequestCount,
  ]);

  return (
    <List className="w-full">
      <HeaderMenuListItem
        href="/channels/me"
        active={
          pathname === "/channels/me"
        }
        icon={
          <BsPersonFill fontSize={20} />
        }
        name="Friends"
        rightContent={
          <Badge
           count={friendRequestCount}
          />
        }
      />

      <HeaderMenuListItem
        href="/nitro"
        active={pathname === "/nitro"}
        icon={<BsStars fontSize={20} />}
        name="Nitro"
        rightContent={<Badge count={0} />}
      />

      <HeaderMenuListItem
        href="/message-requests"
        active={
          pathname ===
          "/message-requests"
        }
        icon={
          <BsInboxFill fontSize={20} />
        }
        name="Message requests"
        rightContent={
          <Badge
            count={messageRequestCount}
          />
        }
      />
    </List>
  );
}