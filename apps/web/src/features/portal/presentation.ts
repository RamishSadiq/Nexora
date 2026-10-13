import type { CSSProperties } from "react";
export type PortalDesign = { theme: "classic" | "neon"; backgroundColor: string; textColor: string; accentColor: string; secondaryAccentColor: string; cornerRadius: number; glowStrength: number; motionEnabled: boolean; motionDurationMs: number };
export const defaultPortalDesign: PortalDesign = { theme: "classic", backgroundColor: "#09090b", textColor: "#fafafa", accentColor: "#50f5d6", secondaryAccentColor: "#9255ff", cornerRadius: 24, glowStrength: 30, motionEnabled: true, motionDurationMs: 220 };
export function portalDesignStyle(design: PortalDesign, brandColor: string): CSSProperties { return { "--portal-brand": brandColor, "--portal-bg": design.backgroundColor, "--portal-fg": design.textColor, "--portal-accent": design.accentColor, "--portal-secondary": design.secondaryAccentColor, "--portal-radius": design.cornerRadius + "px", "--portal-glow": design.glowStrength + "%", "--portal-duration": design.motionDurationMs + "ms" } as CSSProperties; }
export type PortalPresentation = {
  name: string; tagline: string; welcomeText: string; brandColor: string;
  menu: { key: string; label: string; visible: boolean }[];
  dashboardSections: string[];
  pages: { slug: string; title: string; body: string }[];
  design?: PortalDesign | null;
  onboarding?: { allowSelfRegistration: boolean; allowOrganisationRequests: boolean } | null;
};
export const defaultPresentation: PortalPresentation = {
  name: "Member portal", tagline: "Your community. Your space.", welcomeText: "Here is your membership at a glance.", brandColor: "#183a31",
  menu: Object.entries({ dashboard: "Dashboard", memberships: "My memberships", applications: "My applications", organisations: "My organisations", profile: "My profile", notifications: "Notifications" }).map(([key, label]) => ({ key, label, visible: true })),
  dashboardSections: ["summary", "membership", "applications", "links"], pages: [],
};
