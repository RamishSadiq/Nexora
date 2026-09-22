export async function portal<T>(path: string, method = "GET", body?: unknown): Promise<T> {
  const headers: Record<string, string> = {};
  if (method !== "GET") {
    const csrf = await fetch("/api/v1/portal/csrf", { cache: "no-store" });
    if (!csrf.ok) throw Object.assign(new Error("Member sign-in is not configured or is temporarily unavailable."), { status: csrf.status });
    headers["X-CSRF-TOKEN"] = (await csrf.json()).token;
  }
  if (body !== undefined) headers["Content-Type"] = "application/json";
  const response = await fetch(`/api/v1/portal${path}`, { method, headers, cache: "no-store", body: body === undefined ? undefined : JSON.stringify(body) });
  if (!response.ok) {
    if (response.status === 401 && path !== "/login") window.dispatchEvent(new Event("portal-session-expired"));
    const problem = await response.json().catch(() => null);
    throw Object.assign(new Error(response.status === 401 ? "Sign in with an account that has active member access." : response.status === 404 ? "This record is unavailable or you no longer have access to it." : problem?.title ?? "Unable to complete the request. Please try again."), { status: response.status });
  }
  return response.status === 204 ? undefined as T : response.json();
}
