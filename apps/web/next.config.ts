import type { NextConfig } from "next";
import path from "node:path";

const nextConfig: NextConfig = {
  turbopack: { root: path.resolve(__dirname, "../..") },
  distDir: process.env.NEXORA_NEXT_DIST_DIR ?? ".next",
  poweredByHeader: false,
  reactStrictMode: true,
  typedRoutes: true,
  async rewrites() {
    const apiOrigin = process.env.NEXORA_API_URL ?? "http://localhost:5080";

    return [
      {
        source: "/api/:path*",
        destination: `${apiOrigin}/api/:path*`,
      },
    ];
  },
  async headers() {
    return [
      {
        source: "/(.*)",
        headers: [
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
          { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
        ],
      },
    ];
  },
};

export default nextConfig;
