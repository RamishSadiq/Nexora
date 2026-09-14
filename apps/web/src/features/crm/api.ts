export async function crm<T>(path: string, method = "GET", body?: unknown): Promise<T> {
  const headers: Record<string, string> = {};
  if (method !== "GET") {
    const csrf = await fetch("/api/v1/auth/csrf", { credentials: "include", cache: "no-store" });
    if (!csrf.ok) throw new Error("Your session could not be verified. Please sign in again.");
    headers["X-CSRF-TOKEN"] = ((await csrf.json()) as { token: string }).token;
  }
  const multipart = body instanceof FormData;
  if (body && !multipart) headers["Content-Type"] = "application/json";
  const response = await fetch(`/api/v1/crm${path}`, { method, headers, credentials: "include", cache: "no-store", body: body ? multipart ? body : JSON.stringify(body) : undefined });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; errors?: Record<string, string[]> } | null;
    throw new Error(response.status === 401 ? "Your session expired. Sign in again." : response.status === 403 ? "You do not have permission for this action." : response.status === 404 ? "This record is unavailable." : problem?.errors ? Object.values(problem.errors).flat().join(" ") : problem?.title ?? "The service is unavailable. Try again.");
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}
