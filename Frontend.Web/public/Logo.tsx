"use client";
import { useTheme } from "next-themes";
import Image from "next/image";
import teckWhite from "./teck-white.png";
import teck from "./teck.png";

export function Logo({ className = "w-10 h-10 rounded-sm" }: { className?: string }) {
  const { theme } = useTheme();

  return (
    <Image
      src={theme === "dark" ? teckWhite : teck}
      alt="Logo"
      width={1822}
      height={1092}
      className={className}
    />
  );
}