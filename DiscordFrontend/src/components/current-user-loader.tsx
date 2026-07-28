"use client";

import { useEffect, useRef } from "react";
import {
  getCurrentUser,
  mapAuthenticatedUser
} from "@/lib/userApi";
import { useAuthStore } from "@/state/auth";
import { useCurrentUserStore } from "@/state/user";

export default function CurrentUserLoader() {
  const accessToken = useAuthStore(
    state => state.accessToken
  );

  const setCurrentUser = useCurrentUserStore(
    state => state.setCurrentUser
  );

  const loadedTokenRef = useRef<string | null>(
    null
  );

  useEffect(() => {
    if (!accessToken) {
      loadedTokenRef.current = null;
      setCurrentUser(null);
      return;
    }

    if (loadedTokenRef.current === accessToken) {
      return;
    }

    loadedTokenRef.current = accessToken;

    let isCancelled = false;

    const loadCurrentUser = async () => {
      try {
        const backendUser =
          await getCurrentUser(accessToken);

        if (!isCancelled) {
          setCurrentUser(
            mapAuthenticatedUser(backendUser)
          );
        }
      } catch (error) {
        loadedTokenRef.current = null;

        console.error(
          "Cari istifadəçi yüklənmədi:",
          error
        );
      }
    };

    void loadCurrentUser();

    return () => {
      isCancelled = true;
    };
  }, [accessToken, setCurrentUser]);

  return null;
}