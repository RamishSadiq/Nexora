import type { ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "secondary" | "ghost" };

export function Button({ className, variant = "primary", type = "button", ...props }: ButtonProps) {
  return (
    <button
      type={type}
      className={cn(
        "inline-flex h-11 items-center justify-center gap-2 rounded-xl px-4 text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-60",
        variant === "primary" && "bg-[#405bd8] text-white shadow-[0_8px_20px_rgba(64,91,216,.22)] hover:bg-[#334ec5] active:translate-y-px",
        variant === "secondary" && "border border-[#dfe4ec] bg-white text-[#263244] hover:border-[#cdd5e0] hover:bg-[#f8fafc]",
        variant === "ghost" && "text-[#526174] hover:bg-[#f0f3f8] hover:text-[#1c2738]",
        className,
      )}
      {...props}
    />
  );
}
