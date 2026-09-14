import { NextResponse, type NextRequest } from "next/server";

const sessionCookieNames = ["Nexora.Session", "__Host-Nexora.Session"];

export function proxy(request: NextRequest) {
  const hasSession = sessionCookieNames.some((name) => request.cookies.has(name));
  const isLogin = request.nextUrl.pathname === "/login";

  if (!hasSession && !isLogin) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set(
      "returnTo",
      `${request.nextUrl.pathname}${request.nextUrl.search}`,
    );
    return NextResponse.redirect(loginUrl);
  }

  if (hasSession && isLogin) {
    return NextResponse.redirect(new URL("/dashboard", request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    "/((?!api|_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
};
