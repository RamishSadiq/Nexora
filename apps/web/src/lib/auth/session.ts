import { cache } from "react";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";

export type Session = {
  userId: string;
  displayName: string;
  email: string;
  tenantId: string;
  tenantName: string;
  tenantSlug: string;
  roles: string[];
  permissions: string[];
};

export type AccessOverview = {
  users: Array<{ id: string; displayName: string; email: string; isActive: boolean }>;
  roles: Array<{ id: string; name: string; description: string | null; isSystem: boolean }>;
  teams: Array<{ id: string; name: string; memberCount: number }>;
};

const apiOrigin = process.env.NEXORA_API_URL ?? "http://localhost:5080";

async function authenticatedApiFetch(path: string) {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map(({ name, value }) => `${name}=${value}`)
    .join("; ");

  return fetch(`${apiOrigin}${path}`, {
    headers: { cookie: cookieHeader },
    cache: "no-store",
  });
}

export const getSession = cache(async (): Promise<Session | null> => {
  try {
    const response = await authenticatedApiFetch("/api/v1/auth/session");
    return response.ok ? ((await response.json()) as Session) : null;
  } catch {
    return null;
  }
});

export async function requireSession() {
  const session = await getSession();
  if (!session) {
    redirect("/login");
  }
  return session;
}

export async function getAccessOverview(): Promise<AccessOverview | null> {
  try {
    const response = await authenticatedApiFetch("/api/v1/admin/access-overview");
    return response.ok ? ((await response.json()) as AccessOverview) : null;
  } catch {
    return null;
  }
}
