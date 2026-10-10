"use client";

const identifiers = [["facebookId", "Facebook ID"], ["xId", "X ID"], ["linkedInId", "LinkedIn ID"], ["youTubeId", "YouTube ID"], ["onlineCommunities", "Online communities"], ["googleId", "Google ID"]] as const;
const counters = [["numberOfFollowers", "Number of followers"], ["numberOfFollowing", "Number following"], ["numberOfTweetsSent", "Tweets sent"], ["numberOfMentions", "Mentions"], ["numberOfTweetsWithTags", "Tweets with tags"]] as const;
export type ContactSocialMediaDetails = { [K in typeof identifiers[number][0]]: string | null } & { [K in typeof counters[number][0]]: number | null } & { metricsObservedOn: string | null; metricsSource: string | null };
export const emptySocialMedia: ContactSocialMediaDetails = { facebookId: null, xId: null, linkedInId: null, youTubeId: null, onlineCommunities: null, googleId: null, numberOfFollowers: null, numberOfFollowing: null, numberOfTweetsSent: null, numberOfMentions: null, numberOfTweetsWithTags: null, metricsObservedOn: null, metricsSource: null };

export function ContactSocialFields({ value, onChange, disabled, inputClass, hideLegend = false }: { value: ContactSocialMediaDetails; onChange: (value: ContactSocialMediaDetails) => void; disabled: boolean; inputClass: string; hideLegend?: boolean }) {
  return <fieldset disabled={disabled} className="grid gap-4 sm:grid-cols-2">
    <legend className={hideLegend ? "sr-only" : "mb-3 font-semibold"}>Social Media Details</legend>
    {identifiers.map(([key, label]) => <label key={key} className="block text-sm">{label}<input className={inputClass} maxLength={320} value={value[key] ?? ""} onChange={e => onChange({ ...value, [key]: e.target.value })} /></label>)}
    {counters.map(([key, label]) => <label key={key} className="block text-sm">{label}<input className={inputClass} type="number" min={0} max={Number.MAX_SAFE_INTEGER} step={1} value={value[key] ?? ""} onChange={e => onChange({ ...value, [key]: e.target.value === "" ? null : Number(e.target.value) })} /></label>)}
    <label className="block text-sm">Metrics observed on<input className={inputClass} type="date" value={value.metricsObservedOn ?? ""} onChange={e => onChange({ ...value, metricsObservedOn: e.target.value || null })} /></label>
    <label className="block text-sm sm:col-span-2">Metrics source<input className={inputClass} maxLength={160} value={value.metricsSource ?? ""} onChange={e => onChange({ ...value, metricsSource: e.target.value })} /></label>
    <p className="text-sm text-slate-600 sm:col-span-2">Enter profile identifiers or profile URLs. Counts are recorded snapshots; add their observation date and source when known. Leave an unknown count blank.</p>
  </fieldset>;
}
