export const attributeEntities: Record<string, { api: string; types: string[] }> = {
  contacts: { api: "crm", types: ["contact"] },
  accounts: { api: "crm", types: ["account"] },
  membership: { api: "membership", types: ["MembershipProduct", "MemberSubscription"] },
  applications: { api: "membership", types: ["MembershipApplication"] },
  "events-and-learning": { api: "events", types: ["Offering", "EventSession", "Enrollment", "ExamResult"] },
  "sales-and-finance": { api: "finance", types: ["CatalogueProduct", "SalesOrder", "PostedInvoice", "Receipt", "Credit", "Allocation", "Refund"] },
  engagement: { api: "engagement", types: ["Community", "CommunityMember", "CommunityMeeting", "Campaign", "Fund", "Contribution"] },
  work: { api: "work", types: ["WorkItem"] },
};
export const entityLabel = (type: string) => type.replace(/([a-z])([A-Z])/g, "$1 $2");
