import type { Metadata } from "next";
import { ArrowRight, CalendarDays, CheckCircle2, CircleDollarSign, Clock3, ContactRound, MoreHorizontal, TrendingUp, UserRoundPlus, UsersRound } from "lucide-react";

export const metadata: Metadata = { title: "Home" };

const metrics = [
  { label: "Active relationships", value: "12,842", change: "+8.4%", icon: ContactRound, tone: "bg-[#e7edff] text-[#405bd8]" },
  { label: "Current members", value: "8,291", change: "+4.1%", icon: UsersRound, tone: "bg-[#eee8ff] text-[#7351c9]" },
  { label: "Upcoming events", value: "24", change: "6 this month", icon: CalendarDays, tone: "bg-[#def7f2] text-[#147f70]" },
  { label: "Open balance", value: "£84.2k", change: "−6.2%", icon: CircleDollarSign, tone: "bg-[#fff0d8] text-[#ae6a14]" },
];

const activity = [
  { title: "New membership application", detail: "Aisha Mahmood · Professional", time: "8 min", icon: UserRoundPlus, tone: "bg-[#e7edff] text-[#405bd8]" },
  { title: "Invoice payment received", detail: "Halcyon Partners · £1,240.00", time: "26 min", icon: CheckCircle2, tone: "bg-[#e1f8ef] text-[#11825b]" },
  { title: "Event registration updated", detail: "Future Leaders Forum · 3 delegates", time: "1 hr", icon: CalendarDays, tone: "bg-[#eee8ff] text-[#7250c9]" },
];

export default function DashboardPage() {
  return (
    <main className="page-enter mx-auto max-w-[1540px] p-5 sm:p-7 lg:p-9">
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p className="mb-1 text-sm font-medium text-[#667085]">Friday, 11 September</p>
          <h1 className="text-[clamp(1.8rem,3vw,2.4rem)] font-semibold tracking-[-0.045em] text-[#17213a]">Good morning, Ramish</h1>
          <p className="mt-2 text-sm text-[#778296]">Here is what is moving across your organisation today.</p>
        </div>
        <button className="inline-flex h-10 items-center justify-center gap-2 self-start rounded-xl border border-[#dde2e9] bg-white px-3.5 text-sm font-semibold text-[#445064] shadow-sm hover:bg-[#fafbfc] sm:self-auto">Last 30 days<Clock3 className="size-4 text-[#8490a2]" /></button>
      </div>

      <section className="mt-7 grid gap-4 sm:grid-cols-2 xl:grid-cols-4" aria-label="Key metrics">
        {metrics.map((metric) => {
          const Icon = metric.icon;
          return (
            <article key={metric.label} className="rounded-[1.15rem] border border-[#e6e9ef] bg-white p-5 shadow-[0_8px_24px_rgba(20,34,66,.04)]">
              <div className="flex items-start justify-between">
                <span className={`grid size-10 place-items-center rounded-xl ${metric.tone}`}><Icon className="size-[19px]" /></span>
                <button className="grid size-8 place-items-center rounded-lg text-[#98a2b3] hover:bg-[#f5f6f8]" aria-label={`More options for ${metric.label}`}><MoreHorizontal className="size-[18px]" /></button>
              </div>
              <p className="mt-5 text-sm font-medium text-[#707b8c]">{metric.label}</p>
              <div className="mt-1 flex items-end justify-between gap-3">
                <strong className="text-[1.8rem] font-semibold tracking-[-0.05em] text-[#17213a]">{metric.value}</strong>
                <span className="mb-1 inline-flex items-center gap-1 text-xs font-semibold text-[#12845e]">{metric.change.startsWith("+") && <TrendingUp className="size-3.5" />}{metric.change}</span>
              </div>
            </article>
          );
        })}
      </section>

      <section className="mt-5 grid gap-5 xl:grid-cols-[1.5fr_1fr]">
        <article className="overflow-hidden rounded-[1.2rem] border border-[#e6e9ef] bg-white shadow-[0_8px_24px_rgba(20,34,66,.04)]">
          <div className="flex items-center justify-between border-b border-[#edf0f3] px-5 py-4">
            <div><h2 className="font-semibold tracking-[-0.02em] text-[#202b3d]">Relationship growth</h2><p className="mt-0.5 text-xs text-[#8a94a5]">Contacts and active members</p></div>
            <button className="text-sm font-semibold text-[#405bd8] hover:text-[#3048ba]">View report</button>
          </div>
          <div className="px-5 pb-5 pt-7">
            <div className="mb-6 flex items-end gap-8">
              <div><p className="text-xs font-medium text-[#8a94a5]">Total growth</p><p className="mt-1 text-2xl font-semibold tracking-[-0.04em] text-[#1e2a3d]">+1,482</p></div>
              <span className="mb-1 inline-flex items-center gap-1 rounded-full bg-[#e4f7ee] px-2 py-1 text-xs font-semibold text-[#16845e]"><TrendingUp className="size-3.5" />12.4%</span>
            </div>
            <div className="relative h-56 overflow-hidden rounded-xl bg-[linear-gradient(to_bottom,#f8f9fc_1px,transparent_1px)] bg-[size:100%_25%]">
              <svg className="absolute inset-0 h-full w-full" viewBox="0 0 800 220" preserveAspectRatio="none" aria-label="Relationship growth chart">
                <defs><linearGradient id="growthFill" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor="#5d78e8" stopOpacity="0.28"/><stop offset="1" stopColor="#5d78e8" stopOpacity="0"/></linearGradient></defs>
                <path d="M0 178 C80 166 95 125 165 139 C245 155 256 91 335 108 C412 125 436 72 500 82 C572 94 610 42 680 59 C730 70 765 31 800 28 L800 220 L0 220 Z" fill="url(#growthFill)" />
                <path d="M0 178 C80 166 95 125 165 139 C245 155 256 91 335 108 C412 125 436 72 500 82 C572 94 610 42 680 59 C730 70 765 31 800 28" fill="none" stroke="#526de0" strokeWidth="3" strokeLinecap="round" />
              </svg>
              <div className="absolute inset-x-2 bottom-2 flex justify-between text-[10px] font-medium text-[#98a2b3]"><span>Apr</span><span>May</span><span>Jun</span><span>Jul</span><span>Aug</span><span>Sep</span></div>
            </div>
          </div>
        </article>

        <article className="rounded-[1.2rem] border border-[#e6e9ef] bg-white shadow-[0_8px_24px_rgba(20,34,66,.04)]">
          <div className="flex items-center justify-between border-b border-[#edf0f3] px-5 py-4"><div><h2 className="font-semibold tracking-[-0.02em] text-[#202b3d]">Latest activity</h2><p className="mt-0.5 text-xs text-[#8a94a5]">Across your workspace</p></div><button className="grid size-8 place-items-center rounded-lg text-[#98a2b3] hover:bg-[#f5f6f8]" aria-label="Activity options"><MoreHorizontal className="size-[18px]" /></button></div>
          <div className="divide-y divide-[#eef0f3] px-5">
            {activity.map((item) => { const Icon = item.icon; return (
              <div key={item.title} className="flex gap-3.5 py-4"><span className={`grid size-9 shrink-0 place-items-center rounded-xl ${item.tone}`}><Icon className="size-[17px]" /></span><div className="min-w-0 flex-1"><p className="text-sm font-semibold text-[#303c4e]">{item.title}</p><p className="mt-1 truncate text-xs text-[#7b8697]">{item.detail}</p></div><span className="text-[11px] text-[#a0a9b8]">{item.time}</span></div>
            ); })}
          </div>
          <button className="mx-5 mb-5 mt-1 inline-flex items-center gap-2 text-sm font-semibold text-[#405bd8] hover:text-[#3048ba]">See all activity<ArrowRight className="size-4" /></button>
        </article>
      </section>
    </main>
  );
}
