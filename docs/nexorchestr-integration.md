# NexOrchestr AI integration

Nexora's Assistant menu embeds the canonical NexOrchestrAI web application using
the server-side `NEXORCHESTR_APP_URL` setting. Configure it in the web application's
environment to a browser-accessible HTTPS URL on a separate origin. Local
development permits HTTP. Restart Nexora after changing this setting.

The chatbot frontend and orchestration remain owned by NexOrchestrAI. Deploy an
enhancement there and Nexora receives it when the assistant reloads, without
copying code or relying on a sibling checkout. A Git push alone does not deploy
the chatbot. The upstream server must permit Nexora's origin in its embedding
policy and retain its own authentication and provider credentials.

The Nexora page requires a valid session. It does not forward Nexora cookies,
tenant records, or API keys to the chatbot. Shared sign-in and access to Nexora
records are separate integrations; the embedded chatbot does not grant either.

## CRM data bridge disabled (foundation remediation)

The legacy signed query-token bridge is disabled. `/api/v1/assistant/crm-context`
requires the normal `crm.read` policy and returns 503 without reading CRM data.
Anonymous requests return 401. The web application no longer creates or forwards
Nexora access tokens to the iframe. `NEXORCHESTR_SHARED_SECRET` cannot enable the
old bridge and can be removed from Nexora configuration.

Re-enabling record access requires a separately reviewed integration identity,
scopes, live user/tenant authorization, revocation, safe field projection and
negative access tests. Standalone embedding remains available with the upstream
application's own authentication.

Verification: sign in to Nexora, open NexOrchestr AI from either sidebar layout,
send a message with the upstream service available, then deploy an upstream UI
change and reload the Assistant page. With no configured URL the page displays
an unavailable state.
