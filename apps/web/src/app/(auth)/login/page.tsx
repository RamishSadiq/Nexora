import Link from "next/link";
import type { Metadata } from "next";
import { NexoraMark } from "@/components/brand/nexora-mark";
import { LoginForm } from "./login-form";
import { GalaxyBackground } from "./galaxy-background";
import styles from "./login.module.css";

export const metadata: Metadata = { title: "Sign in" };

export default async function LoginPage({ searchParams }: { searchParams: Promise<{ returnTo?: string }> }) {
  const requestedReturnTo = (await searchParams).returnTo;
  const returnTo = requestedReturnTo?.startsWith("/") && !requestedReturnTo.startsWith("//") && !/[\\\x00-\x20]/.test(requestedReturnTo) ? requestedReturnTo : "/dashboard";
  return <main className={styles.page}>
    <GalaxyBackground />
    <header className={styles.header}><div className={styles.brand}><NexoraMark inverse /><span>CRM Login</span></div><Link href="/login/help">Need help?</Link></header>
    <div className={styles.stage}>
      <section className={styles.card} aria-labelledby="login-title">
        <div className={styles.intro}><span className={styles.eyebrow}>Everything connected.</span><h1 id="login-title">Welcome back</h1><p>Relationships, operations and insight.<br />One connected workspace.</p></div>
        <LoginForm returnTo={returnTo} />
      </section>
    </div>
    <footer className={styles.footer}><span>© 2026 Nexora</span><span>Built for modern organisations</span></footer>
  </main>;
}
