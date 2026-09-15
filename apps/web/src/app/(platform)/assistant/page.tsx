import { requireSession } from "@/lib/auth/session";
import { createAssistantAccessToken } from "@/lib/assistant-access";

export default async function AssistantPage() {
  const session = await requireSession();
  const configuredUrl = process.env.NEXORCHESTR_APP_URL;
  const accessToken = createAssistantAccessToken(session);
  let appUrl: string | null = null;
  if (configuredUrl) {
    try {
      const url = new URL(configuredUrl);
      if ((url.protocol === "https:" || (process.env.NODE_ENV !== "production" && url.protocol === "http:")) && !url.username && !url.password) {
        appUrl = url.href;
      }
    } catch { /* Render the unavailable state for invalid configuration. */ }
  }

  return (
    <main className="space-y-4 p-5 sm:p-8">
      <header>
        <h1 className="text-2xl font-semibold text-slate-950">NexOrchestr AI</h1>
        <p className="mt-2 text-sm text-slate-600">Ask your AI team to research, compare, review or plan.</p>
      </header>
      {appUrl ? (
        <iframe
          title="NexOrchestr AI chatbot"
          src={accessToken ? `${appUrl}${appUrl.includes("?") ? "&" : "?"}nexora_access_token=${encodeURIComponent(accessToken)}` : appUrl}
          className="h-[calc(100dvh-220px)] min-h-[480px] w-full rounded-xl border border-slate-200 bg-white"
          sandbox="allow-scripts allow-same-origin allow-forms"
          referrerPolicy="no-referrer"
        />
      ) : (
        <p role="status" className="rounded-xl border border-slate-200 bg-white p-6 text-slate-700">
          The AI assistant is not connected yet. Contact your administrator to enable it.
        </p>
      )}
    </main>
  );
}
