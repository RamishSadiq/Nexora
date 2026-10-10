import { NextResponse, type NextRequest } from "next/server";

const sessionCookieNames = ["Nexora.Session", "__Host-Nexora.Session"];

export async function proxy(request: NextRequest) {
  const pathname = request.nextUrl.pathname;
  // The API resolves only operator-configured host/site pairs, never a submitted tenant ID.
  if (pathname === "/" || pathname === "/sitemap.xml" || /^\/[a-z0-9]+(?:-[a-z0-9]+)*$/.test(pathname) && !["/login", "/portal"].includes(pathname)) {
    const host = request.headers.get("host");
    if (host && !/[\s/\\]/.test(host)) {
      try {
        const response = await fetch(`${process.env.NEXORA_API_URL ?? "http://localhost:5080"}/api/v1/public/resolve-site`, { headers: { Host: host }, cache: "no-store" });
        if (response.ok) {
          const { siteId } = await response.json();
          if (/^[a-f0-9-]{36}$/i.test(siteId)) {
            const url = request.nextUrl.clone(); url.pathname = `/site/${siteId}/${pathname === "/" ? "home" : pathname.slice(1)}`;
            return NextResponse.rewrite(url);
          }
        }
      } catch { /* Existing platform authentication still applies when resolution is unavailable. */ }
    }
  }
  const hasSession = sessionCookieNames.some((name) => request.cookies.has(name));
  const isLogin = pathname === "/login" || pathname === "/login/help";

  const isPublicSite = request.nextUrl.pathname.startsWith("/site/") || request.nextUrl.pathname === "/portal" || request.nextUrl.pathname.startsWith("/portal/");

  if (!hasSession && !isLogin && !isPublicSite) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set(
      "returnTo",
      `${request.nextUrl.pathname}${request.nextUrl.search}`,
    );
    return NextResponse.redirect(loginUrl);
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    "/((?!api|_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
};
