"use client";

import Link from "next/link";
import type { Route } from "next";
import { usePathname } from "next/navigation";
import { useRouter } from "next/navigation";
import { useRef, useState, type ReactNode } from "react";
import { Bell, Blocks, CalendarDays, ChevronDown, ContactRound, ExternalLink, GraduationCap, LayoutDashboard, LogOut, Menu, MessageSquareText, Settings, ShieldCheck, Sparkles, WalletCards, Workflow, X } from "lucide-react";
import { NexoraMark } from "@/components/brand/nexora-mark";
import { cn } from "@/lib/cn";
import type { Session } from "@/lib/auth/session";

import { setupKinds } from "@/features/membership/setup-kinds";

const navigation: Array<{ label: string; href: Route; match: string; icon: typeof LayoutDashboard; children?: Array<{ label: string; href: string; children?: Array<{label: string; href: string}> }> }> = [
  { label: "Home", href: "/dashboard", match: "/dashboard", icon: LayoutDashboard },
  { label: "Relationships", href: "/relationships" as Route, match: "/relationships", icon: ContactRound, children: [{ label: "Contacts", href: "/relationships/contacts" }, { label: "Accounts", href: "/relationships/accounts" }] },
  { label: "Membership", href: "/membership" as Route, match: "/membership", icon: ShieldCheck, children: [{ label: "Members", href: "/membership/members" }, { label: "Applications", href: "/membership/applications" }, { label: "Setup", href: "/membership/setup", children: setupKinds.map(([kind, label]) => ({label, href: `/membership/setup/${kind}`})) }] },
  { label: "Events & learning", href: "/events" as Route, match: "/events", icon: CalendarDays, children: [{ label: "Events", href: "/events/events" }, { label: "Courses", href: "/events/courses" }, { label: "Exams", href: "/events/exams" }] },
  { label: "Sales & finance", href: "/finance" as Route, match: "/finance", icon: WalletCards, children: [{ label: "Orders", href: "/finance/orders" }, { label: "Invoices", href: "/finance/invoices" }, { label: "Receipts", href: "/finance/receipts" }, { label: "Catalogue", href: "/finance/catalogue" }] },
  { label: "Engagement", href: "/engagement" as Route, match: "/engagement", icon: MessageSquareText, children: [{ label: "Communities", href: "/engagement/communities" }, { label: "Campaigns", href: "/engagement/campaigns" }, { label: "Funds", href: "/engagement/funds" }] },
  { label: "Work", href: "/work" as Route, match: "/work", icon: Blocks, children: [{ label: "Tasks", href: "/work/items" }, { label: "Notifications", href: "/work/notifications" }, { label: "Imports", href: "/work/imports" }] },
  { label: "Portal", href: "/portal-management" as Route, match: "/portal-management", icon: Blocks, children: [{ label: "Portal management", href: "/portal-management" }] },
  { label: "Website", href: "/website" as Route, match: "/website", icon: Blocks, children: [{ label: "Website pages and settings", href: "/website" }] },
  { label: "Insights", href: "/insights" as Route, match: "/insights", icon: GraduationCap, children: [{ label: "Reports", href: "/insights" }] },
  { label: "NexOrchestr AI", href: "/assistant" as Route, match: "/assistant", icon: Sparkles, children: [{ label: "Assistant", href: "/assistant" }] },
];

export function AppShell({ children, session }: { children: ReactNode; session: Session }) {
  const pathname = usePathname();
  const router = useRouter();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>({});
  const accountPopover = useRef<HTMLDivElement>(null);
  const hasAdminTools = session.permissions.some(permission => permission.endsWith(".manage") || permission === "crm.configure" || permission === "workflows.read");
  const administrationOpen = openGroups["/settings"] ?? pathname.startsWith("/settings");
  const moduleAccess: Record<string, string> = { "/relationships": "crm.read", "/membership": "membership.read", "/events": "events.read", "/finance": "finance.read", "/engagement": "engagement.read", "/work": "work.read", "/portal-management": "content.read", "/website": "content.read", "/insights": "insights.read", "/assistant": "crm.read" };
  const [isSigningOut, setIsSigningOut] = useState(false);
  const dashboardTheme = pathname === "/dashboard";
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
    <div className={cn("min-h-screen bg-[#f5f7fb] workspace-theme", dashboardTheme && "dashboard-theme")}>
      {mobileOpen && <button className="fixed inset-0 z-40 bg-[#0f1830]/35 backdrop-blur-sm lg:hidden" onClick={() => setMobileOpen(false)} aria-label="Close navigation overlay" />}
      <aside className={cn("fixed inset-y-0 left-0 z-50 flex w-[274px] flex-col border-r border-[#e5e9ef] bg-white transition-transform lg:translate-x-0", mobileOpen ? "visible translate-x-0" : "invisible -translate-x-full lg:visible")}>
        <div className="flex h-[68px] shrink-0 items-center justify-between px-5">
          <NexoraMark inverse />
          <button className="grid size-9 place-items-center rounded-lg text-[#667085] hover:bg-[#f2f4f8] lg:hidden" onClick={() => setMobileOpen(false)} aria-label="Close navigation"><X className="size-5" /></button>
        </div>
        <nav className="min-h-0 flex-1 overflow-y-auto px-3 py-4" aria-label="Primary navigation">
          <p className="mb-2 px-3 text-[10px] font-semibold uppercase tracking-[0.14em] text-[#a0a9b8]">Workspace</p>
          <div className="space-y-1">
            {navigation.filter(item => !moduleAccess[item.match] || session.permissions.includes(moduleAccess[item.match])).map((item) => {
              const active = pathname === item.match || (item.match !== "/dashboard" && pathname.startsWith(item.match));
              const Icon = item.icon;
              const expanded = openGroups[item.match] ?? active;
              return (
                <div key={item.label}>
                  {item.children ? <button type="button" onClick={() => setOpenGroups(groups => ({ ...groups, [item.match]: !expanded }))} aria-expanded={expanded} aria-controls={`navigation-${item.match.slice(1)}`} className={cn("group flex h-11 w-full items-center gap-3 rounded-xl px-3 text-left text-sm font-medium transition", active ? "bg-[#edf1ff] text-[#364fc7]" : "text-[#5c6879] hover:bg-[#f4f6f9] hover:text-[#263244]")}>
                    <Icon className={cn("size-[18px]", active ? "text-[#4b66dd]" : "text-[#8b96a8] group-hover:text-[#5f6e82]")} />
                    <span className="flex-1">{item.label}</span><ChevronDown className={cn("size-3.5 opacity-45 transition-transform", expanded ? "rotate-0" : "-rotate-90")} />
                  </button> : <Link href={item.href} aria-current={active ? "page" : undefined} onClick={() => setMobileOpen(false)} className={cn("group flex h-11 items-center gap-3 rounded-xl px-3 text-sm font-medium transition", active ? "bg-[#edf1ff] text-[#364fc7]" : "text-[#5c6879] hover:bg-[#f4f6f9] hover:text-[#263244]")}>
                    <Icon className={cn("size-[18px]", active ? "text-[#4b66dd]" : "text-[#8b96a8] group-hover:text-[#5f6e82]")} /><span className="flex-1">{item.label}</span>{item.label !== "Home" && <ChevronDown className="size-3.5 -rotate-90 opacity-45" />}
                  </Link>}
                  {item.children && expanded && <div id={`navigation-${item.match.slice(1)}`} className="ml-8 mt-1 space-y-1 border-l border-[#dfe5f5] pl-3">
                    {item.children.map((child, index) => {
                      const childActive = pathname === child.href || pathname.startsWith(`${child.href}/`) || (pathname === item.match && index === 0);
                      if (child.children) {
                        const setupOpen = openGroups[child.href] ?? pathname.startsWith(child.href);
                        return <div key={child.href}><button type="button" aria-expanded={setupOpen} onClick={() => setOpenGroups(groups => ({...groups, [child.href]: !setupOpen}))} className="flex w-full items-center justify-between rounded-lg px-3 py-2 text-xs font-medium text-[#364fc7]">{child.label}<ChevronDown className={cn("size-3.5", !setupOpen && "-rotate-90")} /></button>{setupOpen && <div className="ml-3 border-l border-[#dfe5f5] pl-2">{child.children.map(lookup => <Link key={lookup.href} href={lookup.href as Route} aria-current={pathname === lookup.href ? "page" : undefined} onClick={() => setMobileOpen(false)} className={cn("block rounded-lg px-3 py-2 text-xs", pathname === lookup.href ? "bg-[#f1f4ff] text-[#364fc7]" : "text-[#7b8798] hover:bg-[#f6f7fa]")}>{lookup.label}</Link>)}</div>}</div>;
                      }
                      return <Link key={child.href} href={child.href as Route} aria-current={childActive ? "page" : undefined} onClick={() => setMobileOpen(false)} className={cn("block rounded-lg px-3 py-2 text-xs font-medium", childActive ? "bg-[#f1f4ff] text-[#364fc7]" : "text-[#7b8798] hover:bg-[#f6f7fa]")}>{child.label}</Link>;
                    })}
                  </div>}
                </div>
              );
            })}
          </div>
        </nav>
        <div className="sidebar-account max-h-[45dvh] shrink-0 overflow-y-auto border-t border-[#e9ecf1] p-3">
          {session.permissions.includes("identity.read") && <>
            <button type="button" aria-expanded={administrationOpen} aria-controls="administration-navigation" onClick={() => setOpenGroups(groups => ({...groups, "/settings": !administrationOpen}))} className={cn("flex min-h-11 w-full items-center gap-3 rounded-xl px-3 py-2 text-left text-sm font-medium", pathname.startsWith("/settings") ? "bg-[#edf1ff] text-[#364fc7]" : "text-[#5c6879] hover:bg-[#f4f6f9]")}><Settings className="size-[18px] shrink-0 text-[#8b96a8]" /><span className="flex-1">Administration</span><ChevronDown className={cn("size-3.5 shrink-0", !administrationOpen && "-rotate-90")} /></button>
            {administrationOpen && <div id="administration-navigation" className="ml-8 mt-1 space-y-1 border-l border-[#dfe5f5] pl-3">{[["users", "Users"], ["security-groups", "Security groups"]].map(([section, label]) => <Link key={section} href={`/settings/${section}` as Route} aria-current={pathname === `/settings/${section}` ? "page" : undefined} onClick={() => setMobileOpen(false)} className={cn("block rounded-lg px-3 py-2 text-xs font-medium", pathname === `/settings/${section}` ? "bg-[#f1f4ff] text-[#364fc7]" : "text-[#7b8798] hover:bg-[#f6f7fa]")}>{label}</Link>)}</div>}
          </>}
          <div className="sidebar-profile mt-2">
            <button type="button" popoverTarget="account-options" aria-label={`Account options for ${session.displayName}`} className="profile-trigger flex w-full items-center gap-2.5 rounded-xl px-2 py-3 text-left">
              <span className="profile-avatar grid size-9 shrink-0 place-items-center rounded-full text-[11px] font-semibold" aria-hidden="true">{initials}</span>
              <span className="min-w-0 flex-1">
                <span className="profile-name block truncate text-[11px] font-semibold leading-5" title={session.displayName}>{session.displayName}</span>
                <span className="profile-role block truncate text-[10px] leading-4" title={session.roles[0] ?? session.tenantName}>{session.roles[0] ?? session.tenantName}</span>
              </span>
              <ChevronDown className="size-3.5 shrink-0 opacity-60" />
            </button>
            <div ref={accountPopover} id="account-options" popover="auto" aria-label="Account options" className="account-popover">
              <div className="account-popover-heading">
                <p className="text-[10px] font-semibold uppercase tracking-[0.12em]">Your workspace</p>
                <p className="mt-1 text-xs leading-5">{session.tenantName}</p>
              </div>
              {hasAdminTools && <div className="space-y-1 p-2">
                <Link href={"/automations" as Route} aria-current={pathname.startsWith("/automations") ? "page" : undefined} onClick={() => { accountPopover.current?.hidePopover(); setMobileOpen(false); }} className="account-action">
                  <Workflow className="size-4" /><span>Automations</span>
                </Link>
                {session.permissions.includes("workflows.read") && <button type="button" title="Open WFL Designer in a new window" onClick={() => { window.open("/workflow-designer", "_blank", "popup,width=1440,height=1000,noopener,noreferrer"); accountPopover.current?.hidePopover(); setMobileOpen(false); }} className="account-action">
                  <Blocks className="size-4" /><span className="flex-1">WFL Designer</span><ExternalLink className="size-3.5 opacity-50" />
                </button>}
              </div>}
              <div className="account-signout p-2">
                <button type="button" disabled={isSigningOut} onClick={signOut} className="account-action disabled:opacity-50"><LogOut className="size-4" />{isSigningOut ? "Signing out…" : "Sign out"}</button>
              </div>
            </div>
          </div>
        </div>
      </aside>

      <div className="lg:pl-[274px]">
        <header className="sticky top-0 z-30 flex h-[56px] items-center gap-4 border-b border-[#e5e9ef] bg-white/90 px-4 backdrop-blur-xl sm:px-7">
          <button className="grid size-10 place-items-center rounded-xl border border-[#e2e6ed] text-[#5d697b] lg:hidden" onClick={() => setMobileOpen(true)} aria-label="Open navigation"><Menu className="size-5" /></button>
          <div id="workspace-page-heading" className="flex min-w-0 flex-1 items-center justify-between gap-3" />
          <div className="ml-auto flex items-center gap-1.5">
            {session.permissions.includes("work.read") && <Link href={"/work/notifications" as Route} className="grid size-10 place-items-center rounded-xl text-[#687487] transition hover:bg-[#f2f4f8]" aria-label="Notifications"><Bell className="size-[19px]" /></Link>}
          </div>
        </header>
        <div className="min-h-[calc(100vh-56px)]">{children}</div>
        <footer className="pointer-events-none fixed bottom-3 right-4 z-20 text-right text-[10px] text-white/55 sm:right-7" aria-label="Copyright">
          © 2026 Nexora. All rights reserved.
        </footer>
      </div>
    </div>
  );
}
