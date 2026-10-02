import type { ComponentProps } from "react";
import { cn } from "@/lib/cn";

export function EntityHeading({ className, ...props }: ComponentProps<"h1">) {
  return <h1 {...props} className={cn("inline-block rounded-xl border border-blue-200 bg-blue-100 px-4 py-2 text-3xl font-semibold tracking-tight text-blue-800", className)} />;
}
