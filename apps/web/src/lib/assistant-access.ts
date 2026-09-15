import { createHmac } from "node:crypto";
import type { Session } from "./auth/session";

function base64(value: string) { return Buffer.from(value).toString("base64url"); }
export function createAssistantAccessToken(session: Session) {
  const secret = process.env.NEXORCHESTR_SHARED_SECRET;
  if (!secret) return null;
  const payload = base64(JSON.stringify({ tenantId: session.tenantId, userId: session.userId, exp: Math.floor(Date.now() / 1000) + 900 }));
  const signature = createHmac("sha256", secret).update(payload).digest("base64url");
  return `${payload}.${signature}`;
}
