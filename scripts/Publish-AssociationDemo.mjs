// Local demo only. Creates immutable revisions; never overwrites existing revisions.
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const site = "513f76cf-4cae-40aa-8812-0d9de03c6285";
const base = `/site/${site}`;
const block = (type, heading, text, slug, linkLabel) => ({ type, heading, text, linkUrl: slug ? `${base}/${slug}` : null, linkLabel: linkLabel ?? null });
const pages = [
  { slug: "home", title: "Good things happen when we come together.", description: "Discover Northstar: a place for connection, shared learning and a stronger professional community.", blocks: [
    block("hero", "Find your people. Expand your possibilities.", "Fresh perspectives. Meaningful connections. Space to grow. Northstar brings people together around a shared ambition: moving our community forward.", "membership", "Explore membership"),
    block("feature", "Belong to something bigger", "Meet people who understand your work, share your interests and make room for new ideas.", "about", "Meet Northstar"),
    block("feature", "Keep your curiosity alive", "Explore conversations and learning opportunities shaped around the questions our community cares about.", "events", "Explore our events"),
    block("feature", "Make your voice count", "Bring your experience to the conversation. Help shape what we learn, discuss and do together.", "membership", "Find your place"),
    block("text", "A community built around participation", "The best associations are made by their people. We are creating a place to exchange practical ideas, build lasting relationships and contribute to a shared future."),
    block("cta", "Your next connection starts here.", "Discover what membership could mean for you or your organisation.", "membership", "Discover membership") ] },
  { slug: "about", title: "A shared purpose. A stronger community.", description: "Learn about the Northstar association demo and its community-led purpose.", blocks: [
    block("hero", "Progress starts with people.", "Northstar is a demonstration association built to show how a modern membership community can connect, learn and grow.", "membership", "Discover membership"),
    block("feature", "Connection", "Create space for thoughtful conversations and relationships that last beyond a single meeting."),
    block("feature", "Generosity", "Share what you know, ask questions and welcome different experiences."),
    block("feature", "Participation", "Give members a meaningful role in shaping their community."),
    block("text", "Our people and governance", "This is a demonstration website. An association can introduce its leadership, committees, constitution and governance documents here once those details are confirmed."),
    block("cta", "Get to know us", "Explore the membership experience or find out how to connect with the team.", "contact", "Contact information") ] },
  { slug: "membership", title: "Make room for your next chapter.", description: "Explore individual and organisation membership in the Northstar demonstration website.", blocks: [
    block("hero", "More connection. More opportunity.", "Membership is a place to exchange ideas, develop your interests and participate in a community with a shared purpose.", "contact", "Membership enquiries"),
    block("feature", "For individuals", "A place for people looking to connect with peers, exchange experience and take part in community life."),
    block("feature", "For organisations", "Bring your organisation into the conversation and explore opportunities for colleagues to participate."),
    block("feature", "For emerging talent", "Make new connections, ask questions and learn from different perspectives as you take your next steps."),
    block("text", "How joining will work", "1. Explore the membership options.\n2. Review the association’s confirmed eligibility and fees.\n3. Complete an application when online applications open.\n4. Receive a decision and access your member experience."),
    block("text", "Membership questions", "Are applications open?\nNot yet. This demo does not accept applications or payments.\n\nWhat does membership cost?\nFees, eligibility and included benefits will be published after the association confirms them.\n\nCan I sign in?\nThe member portal is the next delivery phase. No member account is required to explore this website."),
    block("cta", "Want to learn more?", "Read about our purpose and the community we are building.", "about", "About Northstar") ] },
  { slug: "events", title: "Come for the conversation. Leave with a new perspective.", description: "Explore the kinds of gatherings planned for the Northstar demo association.", blocks: [
    block("hero", "Meet. Learn. Exchange ideas.", "From small conversations to shared learning, discover ways a membership community can come together."),
    block("feature", "Community conversations", "Proposed format · Small group discussions focused on shared challenges and practical experience."),
    block("feature", "Learning sessions", "Proposed format · Focused sessions that make space for new skills, questions and different approaches."),
    block("feature", "Member meetups", "Proposed format · Informal opportunities to get to know others in the community."),
    block("text", "The programme is taking shape", "No events are open for booking on this demonstration website. Confirmed dates, locations, accessibility information and booking details will appear when a programme is available."),
    block("cta", "Be part of the community", "Explore how membership can connect you with others.", "membership", "Explore membership") ] },
  { slug: "news", title: "Ideas, updates and community stories.", description: "Updates from the Northstar association demonstration website.", blocks: [
    block("hero", "A new home for our community.", "Demo update · Explore our new public website, learn about membership and discover the purpose behind Northstar.", "about", "Read our story"),
    block("text", "Welcome to the Northstar website", "This demonstration brings association information together in one place. Each page is managed through Nexora, with drafts kept separate from the published website."),
    block("text", "What comes next", "The next phase is a member portal. It will begin with verified access and an own-profile experience before application and membership self-service are connected."),
    block("cta", "Explore what brings us together", "Find out more about the community and its purpose.", "about", "About us") ] },
  { slug: "contact", title: "Every connection starts with a conversation.", description: "Contact information and enquiry guidance for the Northstar demonstration association.", blocks: [
    block("hero", "Let’s find the right place for your question.", "Whether you are exploring membership, interested in events or want to learn about the association, start with the information below."),
    block("feature", "Membership enquiries", "Review membership options, application availability and the answers to common questions.", "membership", "Membership information"),
    block("feature", "Events and participation", "Find out what is planned and when booking information will be available.", "events", "Events information"),
    block("feature", "About the association", "Learn about the purpose and principles behind this community.", "about", "About Northstar"),
    block("text", "Contact details are not yet available", "Northstar is a demonstration association. No inbox, phone number or office address has been configured, and this page does not collect personal information. Verified contact details and an enquiry service must be added before a real public launch.") ] }
];
const q = value => "N'" + value.replaceAll("'", "''") + "'";
let sql = `SET XACT_ABORT ON; BEGIN TRANSACTION;
DECLARE @site uniqueidentifier='${site}', @tenant uniqueidentifier, @actor uniqueidentifier, @page uniqueidentifier, @revision uniqueidentifier, @now datetime2=SYSUTCDATETIME();
SELECT @tenant=TenantId FROM content.Sites WITH (UPDLOCK,HOLDLOCK) WHERE Id=@site AND Name=N'Northstar Association';
IF @tenant IS NULL THROW 50000, 'Expected local Northstar demo site was not found.', 1;
SELECT @actor=Id FROM [identity].Users WHERE TenantId=@tenant AND Email='admin@nexora.local';
IF @actor IS NULL THROW 50000, 'Local demo administrator missing.', 1;
`;
for (const p of pages) {
 sql += `SET @page=NULL; SELECT @page=Id FROM content.Pages WHERE SiteId=@site AND TenantId=@tenant AND Slug=${q(p.slug)};
SET @revision=NEWID();
IF @page IS NULL BEGIN SET @page=NEWID();
INSERT content.Pages(Id,SiteId,Slug,DraftId,PublishedId,TenantId,Version,CreatedAtUtc,UpdatedAtUtc) VALUES(@page,@site,${q(p.slug)},@revision,NULL,@tenant,NEWID(),@now,@now); END;
INSERT content.PageVersions(Id,PageId,Title,Description,BlocksJson,AuthorId,TenantId,Version,CreatedAtUtc,UpdatedAtUtc) VALUES(@revision,@page,${q(p.title)},${q(p.description)},${q(JSON.stringify(p.blocks))},@actor,@tenant,NEWID(),@now,@now);
UPDATE content.Pages SET DraftId=@revision,PublishedId=@revision,Version=NEWID(),UpdatedAtUtc=@now WHERE Id=@page AND TenantId=@tenant;
INSERT content.Publications(Id,PageId,PageVersionId,ActorId,TenantId,Version,CreatedAtUtc,UpdatedAtUtc) VALUES(NEWID(),@page,@revision,@actor,@tenant,NEWID(),@now,@now);
`;
}
sql += "COMMIT;";
const output = path.join(root, "artifacts/website-local/publish-multipage.sql");
fs.mkdirSync(path.dirname(output), { recursive: true });
fs.writeFileSync(output, "\ufeff" + sql, "utf8");
const result = spawnSync("sqlcmd", ["-S", "(localdb)\\MSSQLLocalDB", "-d", "NexoraDev", "-E", "-b", "-i", output], { stdio: "inherit", windowsHide: true });
if (result.error) throw result.error;
if (result.status !== 0) process.exit(result.status ?? 1);
console.log(`Published ${pages.length} local demo pages: http://localhost:3016${base}/home`);
