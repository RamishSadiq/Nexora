import type { Metadata } from "next";
import { ArrowUpRight, Check, ShieldCheck, Sparkles } from "lucide-react";
import { NexoraMark } from "@/components/brand/nexora-mark";
import { LoginForm } from "./login-form";

export const metadata: Metadata = { title: "Sign in" };

const highlights = [
  "One relationship view across every team",
  "Role-aware workspaces that stay focused",
  "Secure, auditable operations by default",
];

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ returnTo?: string }>;
}) {
  const requestedReturnTo = (await searchParams).returnTo;
  const returnTo = requestedReturnTo?.startsWith("/") && !requestedReturnTo.startsWith("//") && !/[\\\x00-\x20]/.test(requestedReturnTo)
    ? requestedReturnTo
    : "/dashboard";
  return (
    <main className="grid min-h-screen bg-white lg:grid-cols-[minmax(420px,0.92fr)_minmax(560px,1.08fr)]">
      <section className="relative flex min-h-screen flex-col px-6 py-7 sm:px-12 lg:px-[clamp(3rem,7vw,7.5rem)] lg:py-9">
        <div className="flex items-center justify-between">
          <NexoraMark />
          <a href="mailto:support@nexora.example" className="rounded-lg px-2 py-1.5 text-sm font-medium text-[#667085] transition hover:bg-[#f5f7fa] hover:text-[#27364a]">
            Need help?
          </a>
        </div>

        <div className="page-enter mx-auto flex w-full max-w-[440px] flex-1 flex-col justify-center py-14">
          <div className="mb-9">
            <span className="mb-4 inline-flex items-center gap-2 rounded-full bg-[#f0f4ff] px-3 py-1.5 text-xs font-semibold text-[#405bd8]">
              <Sparkles className="size-3.5" aria-hidden="true" /> Your connected workspace
            </span>
            <h1 className="text-[clamp(2rem,4vw,2.7rem)] font-semibold leading-[1.08] tracking-[-0.055em] text-[#17213a]">Welcome back</h1>
            <p className="mt-3 max-w-sm text-[15px] leading-6 text-[#667085]">
              Sign in to manage relationships, operations, and insight in one place.
            </p>
          </div>
          <LoginForm returnTo={returnTo} />
        </div>

        <footer className="flex flex-wrap items-center justify-between gap-3 text-xs text-[#8a94a5]">
          <span>© 2026 Nexora</span>
          <div className="flex gap-5"><a className="hover:text-[#405bd8]" href="#">Privacy</a><a className="hover:text-[#405bd8]" href="#">Security</a></div>
        </footer>
      </section>

      <section className="login-aurora relative hidden min-h-screen overflow-hidden p-10 text-white lg:flex lg:flex-col">
        <div className="login-grid absolute inset-0" aria-hidden="true" />
        <div className="absolute -right-20 -top-24 size-80 rounded-full border border-white/10" aria-hidden="true" />
        <div className="absolute -right-4 -top-8 size-52 rounded-full border border-white/10" aria-hidden="true" />
        <div className="relative z-10 flex items-center justify-between">
          <span className="rounded-full border border-white/15 bg-white/10 px-3 py-1.5 text-xs font-medium text-white/80 backdrop-blur">Built for modern organisations</span>
          <ShieldCheck className="size-5 text-white/70" aria-label="Secure platform" />
        </div>

        <div className="relative z-10 m-auto w-full max-w-[620px] py-12">
          <p className="mb-5 text-sm font-semibold uppercase tracking-[0.16em] text-[#aebeff]">Work with clarity</p>
          <h2 className="max-w-xl text-[clamp(2.55rem,4vw,4.6rem)] font-medium leading-[1.02] tracking-[-0.06em]">Everything connected. Nothing in the way.</h2>
          <p className="mt-6 max-w-lg text-base leading-7 text-white/65">Nexora gives every team the context they need, without making them navigate the complexity underneath.</p>
          <div className="mt-10 grid gap-3">
            {highlights.map((highlight) => (
              <div key={highlight} className="flex items-center gap-3 text-sm text-white/85">
                <span className="grid size-6 place-items-center rounded-full bg-white/10"><Check className="size-3.5" aria-hidden="true" /></span>
                {highlight}
              </div>
            ))}
          </div>

          <div className="glass-panel mt-12 grid grid-cols-[1fr_auto] items-end gap-6 rounded-[1.4rem] p-6">
            <div>
              <p className="text-sm font-medium text-white/60">Today across your organisation</p>
              <div className="mt-4 flex items-end gap-8">
                <div><strong className="block text-3xl font-semibold tracking-[-0.05em]">1,284</strong><span className="mt-1 block text-xs text-white/55">active relationships</span></div>
                <div><strong className="block text-3xl font-semibold tracking-[-0.05em]">96%</strong><span className="mt-1 block text-xs text-white/55">tasks on track</span></div>
              </div>
            </div>
            <span className="grid size-10 place-items-center rounded-full bg-white text-[#263a7a]"><ArrowUpRight className="size-5" aria-hidden="true" /></span>
          </div>
        </div>
      </section>
    </main>
  );
}
