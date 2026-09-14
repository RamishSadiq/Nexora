"use client";

import Link from "next/link";
import type { Route } from "next";
import { usePathname } from "next/navigation";
import { useRouter } from "next/navigation";
import { useState, type ReactNode } from "react";
import { Bell, Blocks, CalendarDays, ChevronDown, CircleHelp, ContactRound, GraduationCap, LayoutDashboard, LogOut, Menu, MessageSquareText, PanelLeftClose, Search, Settings, ShieldCheck, Sparkles, WalletCards, X } from "lucide-react";
import { NexoraMark } from "@/components/brand/nexora-mark";
import { cn } from "@/lib/cn";
import type { Session } from "@/lib/auth/session";

const navigation: Array<{ label: string; href: Route; match: string; icon: typeof LayoutDashboard }> = [
  { label: "Home", href: "/dashboard", match: "/dashboard", icon: LayoutDashboard },
  { label: "Relationships", href: "/relationships" as Route, match: "/relationships", icon: ContactRound },
  { label: "Membership", href: "/membership" as Route, match: "/membership", icon: ShieldCheck },
  { label: "Events & learning", href: "/events" as Route, match: "/events", icon: CalendarDays },
  { label: "Sales & finance", href: "/finance" as Route, match: "/finance", icon: WalletCards },
  { label: "Engagement", href: "/engagement" as Route, match: "/engagement", icon: MessageSquareText },
  { label: "Work", href: "/work" as Route, match: "/work", icon: Blocks },
  { label: "Insights", href: "/insights" as Route, match: "/insights", icon: GraduationCap },
];

export function AppShell({ children, session }: { children: ReactNode; session: Session }) {
  const pathname = usePathname();
  const router = useRouter();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [isSigningOut, setIsSigningOut] = useState(false);
  const initials = session.displayName.split(/\s+/).map((part) => part[0]).join("").slice(0, 2).toUpperCase();

  async function signOut() {
    setIsSigningOut(true);
    try {
      const csrf = await fetch("/api/v1/auth/csrf", { credentials: "include" });
      const { token } = (await csrf.json()) as { token: string };
      await fetch("/api/v1/auth/logout", {
        method: "POST",
        credentials: "include",
        headers: { "X-CSRF-TOKEN": token },
      });
    } finally {
      router.replace("/login");
      router.refresh();
    }
  }

  return (
    <div className="min-h-screen bg-[#f5f7fb]">
      {mobileOpen && <button className="fixed inset-0 z-40 bg-[#0f1830]/35 backdrop-blur-sm lg:hidden" onClick={() => setMobileOpen(false)} aria-label="Close navigation overlay" />}
      <aside className={cn("fixed inset-y-0 left-0 z-50 flex w-[274px] flex-col border-r border-[#e5e9ef] bg-white transition-transform lg:translate-x-0", mobileOpen ? "visible translate-x-0" : "invisible -translate-x-full lg:visible")}>
        <div className="flex h-[76px] items-center justify-between px-5">
          <NexoraMark />
          <button className="hidden size-9 place-items-center rounded-lg text-[#8a96aa] transition hover:bg-[#f2f4f8] hover:text-[#344054] xl:grid" aria-label="Collapse sidebar"><PanelLeftClose className="size-[18px]" /></button>
          <button className="grid size-9 place-items-center rounded-lg text-[#667085] hover:bg-[#f2f4f8] lg:hidden" onClick={() => setMobileOpen(false)} aria-label="Close navigation"><X className="size-5" /></button>
        </div>
        <nav className="flex-1 overflow-y-auto px-3 py-3" aria-label="Primary navigation">
          <p className="mb-2 px-3 text-[10px] font-semibold uppercase tracking-[0.14em] text-[#a0a9b8]">Workspace</p>
          <div className="space-y-1">
            {navigation.map((item) => {
              const active = pathname === item.match || (item.match !== "/dashboard" && pathname.startsWith(item.match));
              const Icon = item.icon;
              return (
                <div key={item.label}>
                  <Link href={item.href} onClick={() => setMobileOpen(false)} className={cn("group flex h-11 items-center gap-3 rounded-xl px-3 text-sm font-medium transition", active ? "bg-[#edf1ff] text-[#364fc7]" : "text-[#5c6879] hover:bg-[#f4f6f9] hover:text-[#263244]")}>
                    <Icon className={cn("size-[18px]", active ? "text-[#4b66dd]" : "text-[#8b96a8] group-hover:text-[#5f6e82]")} />
                    <span className="flex-1">{item.label}</span>
                    {item.label !== "Home" && <ChevronDown className="size-3.5 -rotate-90 opacity-45" />}
                  </Link>
                  {item.label === "Relationships" && active && <div className="ml-8 mt-1 space-y-1 border-l border-[#dfe5f5] pl-3">
                    <Link href={"/relationships/contacts" as Route} onClick={() => setMobileOpen(false)} className={cn("block rounded-lg px-3 py-2 text-xs font-medium", pathname === "/relationships/contacts" ? "bg-[#f1f4ff] text-[#364fc7]" : "text-[#7b8798] hover:bg-[#f6f7fa]")}>Contacts</Link>
                    <Link href={"/relationships/accounts" as Route} onClick={() => setMobileOpen(false)} className={cn("block rounded-lg px-3 py-2 text-xs font-medium", pathname === "/relationships/accounts" ? "bg-[#f1f4ff] text-[#364fc7]" : "text-[#7b8798] hover:bg-[#f6f7fa]")}>Accounts</Link>
                  </div>}
                </div>
              );
            })}
          </div>
        </nav>
        <div className="border-t border-[#e9ecf1] p-3">
          <Link className="flex h-11 items-center gap-3 rounded-xl px-3 text-sm font-medium text-[#5c6879] hover:bg-[#f4f6f9]" href={"/settings" as Route}><Settings className="size-[18px] text-[#8b96a8]" />Administration</Link>
          <div className="mt-2 flex items-center gap-3 rounded-xl border border-[#e8ebf0] bg-[#fafbfc] p-3">
            <span className="grid size-9 shrink-0 place-items-center rounded-xl bg-[#dfe6ff] text-xs font-bold text-[#3b55c5]">{initials}</span>
            <div className="min-w-0 flex-1"><p className="truncate text-sm font-semibold text-[#27364a]">{session.displayName}</p><p className="truncate text-xs text-[#8a94a5]">{session.roles[0] ?? session.tenantName}</p></div>
            <button className="grid size-8 place-items-center rounded-lg text-[#98a2b3] hover:bg-white hover:text-[#d34a50] disabled:opacity-50" type="button" disabled={isSigningOut} onClick={signOut} aria-label="Sign out"><LogOut className="size-4" /></button>
          </div>
        </div>
      </aside>

      <div className="lg:pl-[274px]">
        <header className="sticky top-0 z-30 flex h-[76px] items-center gap-4 border-b border-[#e5e9ef] bg-white/90 px-4 backdrop-blur-xl sm:px-7">
          <button className="grid size-10 place-items-center rounded-xl border border-[#e2e6ed] text-[#5d697b] lg:hidden" onClick={() => setMobileOpen(true)} aria-label="Open navigation"><Menu className="size-5" /></button>
          <label className="relative hidden w-full max-w-[480px] md:block">
            <span className="sr-only">Search Nexora</span><Search className="absolute left-3.5 top-1/2 size-[17px] -translate-y-1/2 text-[#98a2b3]" />
            <input className="h-11 w-full rounded-xl border border-transparent bg-[#f4f6f9] pl-10 pr-20 text-sm text-[#263244] outline-none transition placeholder:text-[#98a2b3] focus:border-[#d4dcf8] focus:bg-white focus:ring-4 focus:ring-[#607cf2]/10" placeholder="Search people, organisations, tasks…" />
            <kbd className="absolute right-3 top-1/2 -translate-y-1/2 rounded-md border border-[#dce1e8] bg-white px-1.5 py-0.5 text-[10px] font-medium text-[#8a94a5]">⌘ K</kbd>
          </label>
          <div className="ml-auto flex items-center gap-1.5">
            <button className="hidden h-10 items-center gap-2 rounded-xl bg-[#405bd8] px-4 text-sm font-semibold text-white shadow-[0_7px_16px_rgba(64,91,216,.2)] transition hover:bg-[#334ec5] sm:flex"><Sparkles className="size-4" />Quick create</button>
            <button className="grid size-10 place-items-center rounded-xl text-[#687487] transition hover:bg-[#f2f4f8]" aria-label="Help"><CircleHelp className="size-[19px]" /></button>
            <button className="relative grid size-10 place-items-center rounded-xl text-[#687487] transition hover:bg-[#f2f4f8]" aria-label="Notifications"><Bell className="size-[19px]" /><span className="absolute right-2.5 top-2.5 size-2 rounded-full border-2 border-white bg-[#ef5c62]" /></button>
          </div>
        </header>
        <div className="min-h-[calc(100vh-76px)]">{children}</div>
      </div>
    </div>
  );
}
