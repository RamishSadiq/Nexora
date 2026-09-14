"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import type { Route } from "next";
import { ArrowRight, Eye, EyeOff, LoaderCircle, LockKeyhole } from "lucide-react";
import { Button } from "@/components/ui/button";

export function LoginForm({ returnTo = "/dashboard" }: { returnTo?: string }) {
  const router = useRouter();
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [localEnabled, setLocalEnabled] = useState(false);
  useEffect(() => {
    let active = true;
    fetch("/api/v1/auth/providers", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) throw new Error("Sign-in service unavailable.");
        const provider = (await response.json()) as { localDevelopmentEnabled: boolean };
        if (active) setLocalEnabled(provider.localDevelopmentEnabled);
      })
      .catch(() => { if (active) setError("The sign-in service is currently unavailable."); });
    return () => { active = false; };
  }, []);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);

    const formData = new FormData(event.currentTarget);

    try {
      const csrfResponse = await fetch("/api/v1/auth/csrf", {
        credentials: "include",
      });
      if (!csrfResponse.ok) {
        throw new Error("The sign-in service is currently unavailable.");
      }

      const { token } = (await csrfResponse.json()) as { token: string };
      const response = await fetch("/api/v1/auth/login", {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
          "X-CSRF-TOKEN": token,
        },
        body: JSON.stringify({
          email: formData.get("email"),
          password: formData.get("password"),
          rememberMe: formData.get("rememberMe") === "on",
        }),
      });

      if (!response.ok) {
        const problem = (await response.json().catch(() => null)) as
          | { title?: string }
          | null;
        throw new Error(problem?.title ?? "Unable to sign in.");
      }

      router.replace(returnTo as Route);
      router.refresh();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Unable to sign in.");
      setIsSubmitting(false);
    }
  }

  return (
    <form className="space-y-5" onSubmit={handleSubmit}>
      <p className="text-sm text-[#526174]">{localEnabled ? "Sign in with your local development account." : "Local sign-in is unavailable."} Microsoft Entra single sign-on will be available when connected.</p>
      <div>
        <label className="mb-2 block text-sm font-medium text-[#344054]" htmlFor="email">Work email</label>
        <input className="h-12 w-full rounded-xl border border-[#d9dfe8] bg-white px-3.5 text-[15px] text-[#182230] shadow-sm outline-none transition placeholder:text-[#98a2b3] hover:border-[#bdc7d6] focus:border-[#607cf2] focus:ring-4 focus:ring-[#607cf2]/10" id="email" name="email" type="email" autoComplete="email" placeholder="you@organisation.com" required />
      </div>
      <div>
        <div className="mb-2 flex items-center justify-between">
          <label className="text-sm font-medium text-[#344054]" htmlFor="password">Password</label>

        </div>
        <div className="relative">
          <input className="h-12 w-full rounded-xl border border-[#d9dfe8] bg-white px-3.5 pr-12 text-[15px] text-[#182230] shadow-sm outline-none transition placeholder:text-[#98a2b3] hover:border-[#bdc7d6] focus:border-[#607cf2] focus:ring-4 focus:ring-[#607cf2]/10" id="password" name="password" type={showPassword ? "text" : "password"} autoComplete="current-password" placeholder="Enter your password" required />
          <button className="absolute inset-y-0 right-0 grid w-12 place-items-center rounded-r-xl text-[#7c8798] hover:text-[#405bd8]" type="button" onClick={() => setShowPassword((current) => !current)} aria-label={showPassword ? "Hide password" : "Show password"}>
            {showPassword ? <EyeOff className="size-[18px]" /> : <Eye className="size-[18px]" />}
          </button>
        </div>
      </div>
      <label className="flex cursor-pointer items-center gap-2.5 text-sm text-[#526174]"><input className="size-4 rounded border-[#cfd6e0] accent-[#405bd8]" name="rememberMe" type="checkbox" defaultChecked />Keep me signed in on this device</label>
      {error && <p className="rounded-xl border border-[#f2c7ca] bg-[#fff5f5] px-3.5 py-3 text-sm text-[#a33a40]" role="alert">{error}</p>}
      <Button className="h-12 w-full text-[15px]" type="submit" disabled={isSubmitting || !localEnabled}>
        {isSubmitting ? <><LoaderCircle className="size-[18px] animate-spin" aria-hidden="true" />Signing in…</> : <>Sign in<ArrowRight className="size-[18px]" aria-hidden="true" /></>}
      </Button>
      <div className="relative py-1"><div className="absolute inset-0 flex items-center" aria-hidden="true"><div className="w-full border-t border-[#e6e9ef]" /></div><div className="relative flex justify-center"><span className="bg-white px-3 text-xs font-medium uppercase tracking-[0.08em] text-[#98a2b3]">or continue with</span></div></div>
      <Button className="h-12 w-full" variant="secondary" type="button" disabled><span className="grid size-6 place-items-center rounded-md bg-[#f2f5fb] text-xs font-bold text-[#335aa7]">M</span>Microsoft SSO · not connected</Button>
      <p className="flex items-center justify-center gap-2 rounded-xl bg-[#f7f8fb] px-3 py-2.5 text-center text-xs leading-5 text-[#778296]"><LockKeyhole className="size-3.5 shrink-0" aria-hidden="true" />Secure, tenant-isolated access with auditable sign-in activity.</p>
    </form>
  );
}
