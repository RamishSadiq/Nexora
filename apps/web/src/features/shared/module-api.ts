export async function moduleApi<T>(module: string, path: string, method = "GET", body?: unknown): Promise<T> {
  const headers: Record<string,string> = {};
  if (method !== "GET") {
    const response = await fetch("/api/v1/auth/csrf", { credentials: "include", cache: "no-store" });
    if (!response.ok) throw new Error("Your session could not be verified. Sign in again.");
    headers["X-CSRF-TOKEN"] = ((await response.json()) as { token: string }).token;
  }
  if (body !== undefined) headers["Content-Type"] = "application/json";
  const response = await fetch(`/api/v1/${module}${path}`, { method, headers, credentials:"include", cache:"no-store", body: body === undefined ? undefined : JSON.stringify(body) });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; errors?: Record<string,string[]> } | null;
    throw new Error(response.status === 401 ? "Your session expired. Sign in again." : response.status === 403 ? "You do not have permission for this action." : response.status === 404 ? "This record is unavailable." : problem?.errors ? Object.values(problem.errors).flat().join(" ") : problem?.title ?? "Unable to complete the request.");
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}
