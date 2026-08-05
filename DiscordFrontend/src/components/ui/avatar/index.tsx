import { clsx } from "@/lib/utils";
import { StaticUserStatuses } from "@/lib/entities/user";
import Image from "next/image";
import { BsDiscord } from "react-icons/bs";
import StatusBadge from "@/components/ui/badge/status-badge";

interface AvatarProps {
  status?: StaticUserStatuses;
  size?: "sm" | "md" | "lg";
  src?: string | null;
  alt: string;
  className?: string;
}

export default function Avatar({
  status,
  size = "md",
  src,
  alt,
  className,
}: AvatarProps) {
  return (
    <div
      className={clsx(
        "relative flex h-8 w-8 items-center justify-center rounded-full text-white",
        size === "sm" && "h-6 w-6",
        size === "md" && "h-8 w-8",
        size === "lg" && "h-12 w-12",
        src ? "bg-white/5" : "bg-orange-400",
        className,
      )}
    >
      {src ? (
        <Image
  className="rounded-full object-cover"
  unoptimized
  priority
  src={src}
  alt={alt}
  fill
  sizes={
    size === "sm"
      ? "24px"
      : size === "lg"
        ? "48px"
        : "32px"
  }
/>
      ) : (
        <BsDiscord fontSize={18} />
      )}
      {status && (
        <StatusBadge className="absolute -bottom-1 -right-1" status={status} />
      )}
    </div>
  );
}
