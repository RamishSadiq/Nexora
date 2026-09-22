"use client";
export default function SiteError({ reset }: { reset: () => void }) {
  return <main className="mx-auto max-w-xl p-10"><h1 className="text-2xl font-semibold">This website is temporarily unavailable</h1><p className="my-4">Please try again in a moment.</p><button className="rounded-lg border px-4 py-2" onClick={reset}>Try again</button></main>;
}
