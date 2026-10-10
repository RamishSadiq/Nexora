import type { Page } from "@playwright/test";

// These journeys share one disposable server/IP and its real fixed-window
// login limiter. Only 429 is retried; credential/authorization failures remain
// visible to the caller's existing assertions.
export async function retryLogin<T extends { status(): number }>(attempt: () => Promise<T>, isLogin = true): Promise<T> {
  const first = await attempt();
  if (!isLogin || first.status() !== 429) return first;
  await new Promise(resolve => setTimeout(resolve, 61_000));
  return attempt();
}

export async function submitLogin(page: Page, scheme: "auth" | "portal" = "auth") {
  return retryLogin(async () => {
    const response = page.waitForResponse(response =>
      new URL(response.url()).pathname === `/api/v1/${scheme}/login` && response.request().method() === "POST");
    await page.getByRole("button", { name: "Sign in", exact: true }).click();
    return response;
  });
}
