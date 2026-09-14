import { ShieldCheck, UsersRound, UserRoundCog } from "lucide-react";
import { getAccessOverview, requireSession } from "@/lib/auth/session";

export default async function AdministrationPage() {
  const session = await requireSession();
  const overview = await getAccessOverview();

  if (!overview) {
    return (
      <main className="p-6 sm:p-8">
        <div className="mx-auto max-w-6xl rounded-2xl border border-[#ead4a8] bg-[#fffaf0] p-6 text-[#75551e]">
          Your account does not have permission to view access administration.
        </div>
      </main>
    );
  }

  const cards = [
    { label: "Active users", value: overview.users.filter((user) => user.isActive).length, icon: UsersRound },
    { label: "Roles", value: overview.roles.length, icon: UserRoundCog },
    { label: "Teams", value: overview.teams.length, icon: ShieldCheck },
  ];

  return (
    <main className="p-5 sm:p-8">
      <div className="mx-auto max-w-6xl space-y-7">
        <div>
          <p className="text-sm font-semibold text-[#405bd8]">{session.tenantName}</p>
          <h1 className="mt-1 text-3xl font-semibold tracking-[-0.04em] text-[#17213a]">Access administration</h1>
          <p className="mt-2 text-sm text-[#667085]">Tenant-scoped users, roles, and teams from the identity service.</p>
        </div>

        <section className="grid gap-4 sm:grid-cols-3">
          {cards.map(({ label, value, icon: Icon }) => (
            <div className="rounded-2xl border border-[#e5e9ef] bg-white p-5 shadow-sm" key={label}>
              <div className="flex items-center justify-between"><span className="text-sm text-[#667085]">{label}</span><Icon className="size-5 text-[#607cf2]" /></div>
              <strong className="mt-4 block text-3xl font-semibold tracking-[-0.04em] text-[#17213a]">{value}</strong>
            </div>
          ))}
        </section>

        <section className="overflow-hidden rounded-2xl border border-[#e5e9ef] bg-white shadow-sm">
          <div className="border-b border-[#e9ecf1] px-5 py-4"><h2 className="font-semibold text-[#263244]">Users</h2></div>
          <div className="divide-y divide-[#eef0f3]">
            {overview.users.map((user) => (
              <div className="flex flex-wrap items-center gap-3 px-5 py-4" key={user.id}>
                <span className="grid size-9 place-items-center rounded-xl bg-[#edf1ff] text-xs font-bold text-[#405bd8]">{user.displayName.slice(0, 2).toUpperCase()}</span>
                <div className="min-w-0 flex-1"><p className="font-medium text-[#27364a]">{user.displayName}</p><p className="text-sm text-[#7c8798]">{user.email}</p></div>
                <span className="rounded-full bg-[#edf8f1] px-2.5 py-1 text-xs font-semibold text-[#347452]">{user.isActive ? "Active" : "Inactive"}</span>
              </div>
            ))}
          </div>
        </section>

        <div className="grid gap-5 lg:grid-cols-2">
          <section className="rounded-2xl border border-[#e5e9ef] bg-white p-5 shadow-sm"><h2 className="font-semibold text-[#263244]">Roles</h2><div className="mt-4 space-y-3">{overview.roles.map((role) => <div className="rounded-xl bg-[#f7f8fb] p-3" key={role.id}><p className="text-sm font-semibold text-[#344054]">{role.name}</p><p className="mt-1 text-xs text-[#7c8798]">{role.description}</p></div>)}</div></section>
          <section className="rounded-2xl border border-[#e5e9ef] bg-white p-5 shadow-sm"><h2 className="font-semibold text-[#263244]">Teams</h2><div className="mt-4 space-y-3">{overview.teams.map((team) => <div className="flex items-center justify-between rounded-xl bg-[#f7f8fb] p-3" key={team.id}><p className="text-sm font-semibold text-[#344054]">{team.name}</p><span className="text-xs text-[#7c8798]">{team.memberCount} member{team.memberCount === 1 ? "" : "s"}</span></div>)}</div></section>
        </div>
      </div>
    </main>
  );
}
