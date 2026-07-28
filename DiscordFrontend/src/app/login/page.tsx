"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { login } from "@/lib/authApi";
import { useAuthStore } from "@/state/auth";

export default function LoginPage() {
  const router = useRouter();
  const setTokens = useAuthStore(
    state => state.setTokens
  );

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errorMessage, setErrorMessage] =
    useState<string | null>(null);
  const [isLoading, setIsLoading] =
    useState(false);

  const handleSubmit = async (
    event: FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault();
    setErrorMessage(null);

    if (!email.trim() || !password) {
      setErrorMessage(
        "Email və şifrə daxil edilməlidir."
      );
      return;
    }

    try {
      setIsLoading(true);

      const response = await login({
  emailOrUsername: email.trim(),
  password
});

      setTokens(
        response.accessToken,
        response.refreshToken
      );

      router.push("/channels/me");
    } catch (error) {
      setErrorMessage(
        error instanceof Error
          ? error.message
          : "Login zamanı xəta baş verdi."
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="flex min-h-screen items-center justify-center bg-[#313338] px-4">
      <section className="w-full max-w-md rounded-md bg-[#2b2d31] p-8 text-white shadow-2xl">
        <div className="mb-6 text-center">
          <h1 className="text-2xl font-bold">
            Welcome back!
          </h1>

          <p className="mt-2 text-sm text-gray-400">
            Discord Clone hesabına daxil ol
          </p>
        </div>

        <form
          onSubmit={handleSubmit}
          className="space-y-5"
        >
          <div>
            <label
              htmlFor="email"
              className="mb-2 block text-xs font-bold uppercase text-gray-300"
            >
              Email
            </label>

            <input
              id="email"
              type="text"
              autoComplete="username"
              value={email}
              onChange={event =>
                setEmail(event.target.value)
              }
              disabled={isLoading}
              className="w-full rounded bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-[#5865f2]"
            />
          </div>

          <div>
            <label
              htmlFor="password"
              className="mb-2 block text-xs font-bold uppercase text-gray-300"
            >
              Şifrə
            </label>

            <input
              id="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={event =>
                setPassword(event.target.value)
              }
              disabled={isLoading}
              className="w-full rounded bg-[#1e1f22] px-3 py-3 text-sm outline-none focus:ring-2 focus:ring-[#5865f2]"
            />
          </div>

          {errorMessage && (
            <p className="rounded bg-red-500/10 px-3 py-2 text-sm text-red-400">
              {errorMessage}
            </p>
          )}

          <button
            type="submit"
            disabled={isLoading}
            className="w-full rounded bg-[#5865f2] px-4 py-3 font-semibold transition hover:bg-[#4752c4] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isLoading
              ? "Daxil olunur..."
              : "Daxil ol"}
          </button>
        </form>
      </section>
    </main>
  );
}