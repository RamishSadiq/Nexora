// Editable starter content for the local association demonstration. No customer claims or live services.
export function associationPages(siteId) {
  const url = slug => `/site/${siteId}/${slug}`;
  const block = (type, heading, text, linkLabel, linkUrl) => ({ type, heading, text, ...(linkUrl ? { linkLabel, linkUrl } : {}) });
  const hero = (...args) => block("hero", ...args);
  const text = (...args) => block("text", ...args);
  const feature = (...args) => block("feature", ...args);
  const cta = (...args) => block("cta", ...args);
  const page = (slug, label, title, description, blocks, visible = true) => ({ slug, label, title, description, blocks, visible });
  return [
    page("home", "Home", "Good connections. Greater possibilities.", "A place for people and organisations to connect, discover membership and take their next step together.", [
      hero("A stronger community starts with you.", "Bring your experience. Find a shared purpose. Discover an association built around the people and organisations that make it matter.", "Explore membership", url("membership")),
      feature("For individuals", "Keep your details, applications and membership information together in your own member space.", "Your membership journey", url("member-guide")),
      feature("For organisations", "Connect your organisation with its representatives and manage applications through authorised access.", "Company membership explained", url("company-guide")),
      feature("For shared progress", "Start with practical guidance, clear next steps and a better understanding of how membership works.", "Explore the resources", url("resources")),
      text("One community. Many ways to take part.", "Whether you are exploring membership for yourself or representing an organisation, there is a clear place to begin. Learn about the association, understand the application process and use your member space to follow your progress.", "Get to know us", url("about")),
      cta("Already have member access?", "Your profile, applications and updates are ready in the member portal.", "Open member portal", "/portal"),
      text("Explore the demonstration", "This is an illustrative association website powered by Nexora. Content and member accounts are for demonstration; no payment is collected and no live events or contact service are advertised.")
    ]),
    page("about", "About", "Built around people. Connected by purpose.", "Discover the purpose and approach behind our demonstration association community.", [
      hero("A place to belong and contribute.", "An association is strongest when its people can participate with confidence. Our approach makes the journey from first interest to active membership clear and accessible.", "Find your next step", url("membership")),
      text("Our purpose", "We bring individual members and organisations into a shared community. This demonstration shows how an association can explain its purpose, welcome applications and keep members connected through one consistent experience."),
      feature("Clarity", "Understand the membership journey before you apply, with guidance you can return to at any time."),
      feature("Participation", "Stay involved as an individual or as an authorised representative of your organisation."),
      feature("Connection", "Keep your contact details and membership updates together, so the association can work with the right information."),
      cta("Start with the essentials", "Learn how applications, organisation access and member updates work.", "Read our frequently asked questions", url("faq"))
    ]),
    page("membership", "Membership", "Find your place in the community.", "Explore individual and organisation membership and learn how to apply through the member portal.", [
      hero("Your next chapter starts here.", "Explore the right route for you, review the available offering and follow your application from submission to decision.", "Open member portal", "/portal"),
      feature("Individual membership", "Apply in your own name. Save a draft, provide the requested information and return when you are ready.", "Read the individual guide", url("member-guide")),
      feature("Organisation membership", "Apply on behalf of an organisation when its access has been assigned to you by the association team.", "Read the organisation guide", url("company-guide")),
      feature("Already a member?", "Review your profile and membership status, follow existing applications and read your updates.", "Go to your member space", "/portal"),
      text("How the application works", "1. Sign in using the member access provided by the association.\n2. Choose an available offering and review its terms and quoted amount.\n3. Save your progress and add any requested supporting evidence.\n4. Submit your application and keep the receipt.\n5. Follow the decision and any updates in your portal."),
      text("Before you apply", "The offering, eligibility requirements and terms shown in the portal are the source of truth for your application. Submission does not guarantee approval. Our local demonstration offering has no fee; online payment is not available in this demonstration."),
      cta("Need help choosing a route?", "Find answers to common questions about access, applications and organisation representatives.", "Membership questions", url("faq"))
    ]),
    page("events", "Events", "Make room for meaningful connections.", "Find out about association events and how future opportunities will be announced.", [
      hero("Conversations that move us forward.", "Events can bring members together to exchange ideas, meet peers and explore the questions that matter to their community."),
      text("Upcoming events", "There are no public events announced on this demonstration website. Confirmed dates, venues, eligibility and booking details will appear here when an event is published. No bookings or payments are being taken."),
      feature("Learn together", "Look out for opportunities to explore practical topics and learn from others in the community."),
      feature("Meet your peers", "Future networking sessions can make space for new introductions and shared experience."),
      feature("Have your say", "Association conversations are an opportunity to hear different perspectives and contribute your own."),
      cta("Explore while you are here", "Our membership guides explain how to get started and keep your member information up to date.", "Browse resources", url("resources"))
    ]),
    page("resources", "Resources", "Useful guidance. Clear next steps.", "Practical guides to individual membership, organisation access and using your member portal.", [
      hero("A little guidance goes a long way.", "Find answers before you apply, understand your responsibilities as a representative and make the most of your member space."),
      feature("Getting started", "From your first sign-in to your application receipt: a step-by-step guide for individual members.", "Read the member guide", url("member-guide")),
      feature("Representing an organisation", "Understand delegated access, organisation applications and what to do when your role changes.", "Read the company guide", url("company-guide")),
      feature("Common questions", "Find quick answers about drafts, decisions, documents and keeping your information current.", "Read the FAQs", url("faq")),
      cta("Put the guidance into practice", "Sign in to view your own profile and the membership options available to you.", "Open member portal", "/portal")
    ]),
    page("contact", "Contact", "Let’s find the right next step.", "Find membership guidance, account-access information and help with your application.", [
      hero("Start with what you need.", "Whether you are exploring membership or checking an application, these routes will help you find the right information."),
      feature("Membership questions", "Learn about applying as an individual or on behalf of your organisation.", "Explore membership", url("membership")),
      feature("An existing application", "Sign in to check your saved drafts, submitted applications, receipts and member updates.", "Check your application", "/portal"),
      feature("Using your member space", "Understand access, evidence and profile updates with our practical answers.", "Find an answer", url("faq")),
      text("Account access", "Member accounts in this demonstration are provided by the association administrator. If you have not received access, contact the person who invited you to view the demonstration. Self-registration and email password recovery are not enabled here."),
      text("Contact details for this demonstration", "This sample association does not operate a public contact inbox, telephone line or office. No enquiry form is connected. The association’s verified contact details can be added here by its content team before public launch.")
    ]),
    page("faq", "FAQs", "Your questions, answered.", "Answers about portal access, membership applications, organisation permissions and supporting evidence.", [
      text("How do I get access?", "For this demonstration, the administrator provides your member account. Use the Member portal link to sign in. Public self-registration and email recovery are not enabled."),
      text("Can I save an application and finish it later?", "Yes. Save your application as a draft and return to it in the portal. Review your information and the offering terms before submitting. An unsubmitted draft can be discarded."),
      text("What happens after I submit?", "Your application is available to association staff for review. You can return to its receipt and follow its status in the portal. The decision appears there once staff have reviewed it."),
      text("Can I apply for my organisation?", "Only when staff have granted you access to that organisation, including permission to submit applications. Sharing an email domain or being listed as a contact does not automatically grant access."),
      text("Why is an uploaded document not immediately available?", "Supporting evidence is checked before download is allowed. Files awaiting a security scan remain unavailable. Staff can review the status; uploading again will not bypass the scan."),
      text("Can I update my contact details?", "Yes. You can update the profile fields made available in your portal. These changes are shared with the association’s records. Staff-only information is not editable by members."),
      text("Can I pay online?", "Not in this demonstration. The local demonstration offering has no fee. Always check the quoted amount and terms for the offering you are viewing."),
      cta("Ready to continue?", "Return to your member space to view your own information.", "Open member portal", "/portal")
    ], false),
    page("member-guide", "Member guide", "Your membership journey, step by step.", "A practical guide to signing in, updating your profile and submitting a membership application.", [
      hero("Start confidently. Stay informed.", "Everything begins with the member access provided by your association. Keep those details private and sign out when using a shared device.", "Open member portal", "/portal"),
      text("01 — Check your profile", "Sign in and review your name and correspondence details. Update the available fields so staff have the right information when they review your application."),
      text("02 — Review your membership options", "Choose an available offering. Read its terms and quoted amount before continuing. Your application records the terms you accepted."),
      text("03 — Prepare and submit", "Save your draft while you gather any requested evidence. Upload only the documents requested, review your answers and submit when ready. Keep the receipt for reference."),
      text("04 — Follow your progress", "Return to your applications to check the current status. Your member inbox holds relevant updates. Staff review determines the outcome; a submission is not an automatic approval."),
      cta("Applying for a company instead?", "Organisation applications need explicit representative access.", "Read the organisation guide", url("company-guide"))
    ], false),
    page("company-guide", "Organisation guide", "Represent your organisation with confidence.", "Learn how authorised representatives use organisation access and submit company membership applications.", [
      hero("The right access. The right organisation.", "Organisation membership connects a company with its authorised representatives while keeping access under the association’s control."),
      text("01 — Confirm your access", "Ask your association administrator to confirm which organisation you represent. Access is explicitly assigned and can be revoked. A matching email address or contact association alone is not enough."),
      text("02 — Choose the organisation", "Sign in and select the organisation available to you. Check its identity before starting any application. You will only see the organisation actions that your access permits."),
      text("03 — Apply on its behalf", "If submission is enabled for you and an organisation offering is available, review the terms, complete the application and attach requested evidence. Keep the organisation application receipt."),
      text("04 — Keep responsibilities current", "If your role changes or you leave the organisation, tell the association team so it can update your access. The organisation’s membership and your individual membership are separate records."),
      cta("Check your organisation access", "Open your member space to see the organisations assigned to you.", "Open member portal", "/portal")
    ], false)
  ];
}

export async function prepareAssociationWebsite({ get, send, site }) {
  const state = await get("/api/v1/content/site");
  const created = [], preserved = [];
  // Only upgrade the exact original seed page; never overwrite a user's draft or custom homepage.
  for (const [order, definition] of associationPages(site.id).entries()) {
    const existing = state.pages.find(p => p.slug === definition.slug);
    let detail;
    if (existing) {
      detail = await get(`/api/v1/content/pages/${existing.id}`);
      const first = detail.revision.blocks[0];
      const original = definition.slug === "home" && detail.page.draftId === detail.page.publishedId && detail.revision.title === "Welcome to the local association" && detail.revision.description === "Test the integrated association website and member portal." && detail.revision.blocks.length === 1 && first.type === "hero" && first.heading === "Your association, connected" && first.text === "Explore membership and update your profile through the member portal." && first.linkLabel === "Member portal" && first.linkUrl === "/portal" && !first.mediaId;
      if (!original) { preserved.push(definition.slug); continue; }
      detail = await send(`/api/v1/content/pages/${existing.id}`, { ...definition, version: detail.page.version }, "PUT");
    } else detail = await send("/api/v1/content/pages", definition);
    const configured = await send(`/api/v1/content/pages/${detail.page.id}/navigation`, { version: detail.page.version, label: definition.label, order, visible: definition.visible }, "PUT");
    await send(`/api/v1/content/pages/${detail.page.id}/publish`, { version: configured.version });
    created.push(definition.slug);
  }
  if (site.name === "Nexora Local Association" && site.tagline === "Explore CRM, Portal and CMS together.") {
    const current = (await get("/api/v1/content/site")).site;
    await send("/api/v1/content/site", { ...current, name: "Nexora Association", tagline: "People. Purpose. Possibility.", brandColor: current.brandColor ?? "#183a31" }, "PUT");
  }
  return { created, preserved };
}
