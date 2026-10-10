import type { ComponentProps } from "react";
import { cn } from "@/lib/cn";

export function EntityHeading({ className, ...props }: ComponentProps<"h1">) {
  return <h1 {...props} className={cn("inline-block rounded-lg border border-[#e6dbe5] bg-[#faf5f9] px-3 py-1.5 text-xl font-semibold leading-7 tracking-[-0.025em] text-[#59374f]", className)} />;
}
