export type PortalPresentation = {
  name: string; tagline: string; welcomeText: string; brandColor: string;
  menu: { key: string; label: string; visible: boolean }[];
  dashboardSections: string[];
  pages: { slug: string; title: string; body: string }[];
};
export const defaultPresentation: PortalPresentation = {
  name: "Member portal", tagline: "Your community. Your space.", welcomeText: "Here is your membership at a glance.", brandColor: "#183a31",
  menu: Object.entries({ dashboard: "Dashboard", memberships: "My memberships", applications: "My applications", organisations: "My organisations", profile: "My profile", notifications: "Notifications" }).map(([key, label]) => ({ key, label, visible: true })),
  dashboardSections: ["summary", "membership", "applications", "links"], pages: [],
};
