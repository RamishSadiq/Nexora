"""Prepare a local acquisition queue and drafts. Never sends or creates Gmail drafts."""
import hashlib
import html
import json
from datetime import datetime, timedelta, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MARKET = ROOT / "docs/market"
OUT = MARKET / "acquisition"
NOW = datetime.now(timezone.utc)
STAMP = NOW.isoformat()


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_suffix(path.suffix + ".tmp")
    temp.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temp.replace(path)


def main():
    state = read(MARKET / "us/daily/STATE.json")
    source = MARKET / "middle-east/research/2026-10-10/hourly/07-utc/PRIVATE-BUSINESS-SCREENING.json"
    screening = read(source)
    held = read(source.parent / "held-drafts.json")
    contacted = {c["domain"].lower() for c in state["previouslyContacted"]}
    suppressions = read(MARKET / "OUTREACH-SUPPRESSIONS.json")["entries"]
    attachment = ROOT / "docs/market/pitch/nexora-product-demo-male.mp4"
    digest = hashlib.sha256(attachment.read_bytes()).hexdigest().upper()
    if attachment.stat().st_size != 1945313 or digest != "3D079132720D56CE1A97A394643BD84297D8491CA70CDBF8FB06F9227C01D8E7":
        raise SystemExit("Attachment changed: review before preparing campaign drafts")

    path = OUT / "PIPELINE.json"
    pipeline = read(path) if path.exists() else {"schemaVersion": 1, "createdAt": STAMP, "opportunities": []}
    by_domain = {o["domain"].lower(): o for o in pipeline["opportunities"]}
    queue = []
    for lead in screening["candidates"]:
        domain = lead["domain"].lower()
        old = by_domain.get(domain)
        scores = {
            "need": 1 if lead.get("retrieval") == "full official page" else 0,
            "capabilityFit": 2 if lead.get("fit") else 0,
            "buyerAccess": 0,
            "timing": 0,
        }
        # This source contains signals only, no confirmed buyer or active request.
        reasons = ["Current buyer need and provider appointment unconfirmed", "Relevant decision-maker not verified"]
        if not lead.get("permissionEvidence") or lead.get("outreachLegalBasis") == "not_cleared":
            reasons.append("Recipient/channel lawful contact route unresolved")
        if not lead.get("email"):
            reasons.append("Official recipient email not verified")
        if domain in contacted:
            reasons.append("Organisation already contacted")
        for entry in suppressions:
            if entry.get("email", "").lower() == (lead.get("email") or "").lower() or entry.get("domain", "").lower() == domain:
                reasons.append("Suppressed recipient/organisation")
        item = {
            "organisation": lead["organisation"], "domain": domain, "country": lead["country"],
            "offerType": "website_cms", "source": lead["url"],
            "sourceCheckedAt": lead["checkedAt"], "observedFact": lead["observation"],
            "problemHypothesis": "Staff may need to maintain public service or project-information pages",
            "buyerConfirmedProblem": None, "buyerRole": None, "buyerContact": lead.get("email"),
            "currentProvider": "unknown", "budget": None, "currency": None, "decisionDate": None,
            "scores": scores, "researchPriorityScore": sum(scores.values()), "scoreMaximum": 12,
            "scoreMeaning": "Need/fit/buyer/timing each 0-3; priority only, not purchase probability",
            "sendEligible": False, "holdReasons": reasons, "owner": "Ramish Qureshi",
            "nextAction": "Verify an open requirement, buyer role and applicable contact route; ask founder for an existing introduction",
            "reviewDue": (NOW.date() + timedelta(days=3)).isoformat(), "stage": "research",
            "consentStatus": lead.get("consentStatus"), "permissionEvidence": lead.get("permissionEvidence", []),
            "outreachLegalBasis": lead.get("outreachLegalBasis"),
            "history": [{"at": STAMP, "event": "Baseline from dated private-business screening; no send eligibility inferred"}],
        }
        if old is None:
            pipeline["opportunities"].append(item)
        # Existing buyer evidence and stages belong to the sales record, never reset them.
        review_item = dict(old) if old is not None else item
        review_item["sendEligible"] = False
        review_item["eligibilityAssessment"] = "Local preparation utility has not performed live sending checks"
        queue.append(review_item)
    pipeline["reviewedAt"] = STAMP
    write(path, pipeline)
    write(OUT / "PRIORITY-QUEUE.json", {"generatedAt": STAMP, "basis": str(source.relative_to(ROOT)), "queue": sorted(queue, key=lambda x: -x["researchPriorityScore"]), "sendEligibleCount": 0})

    drafts = []
    revised_path = OUT / "REVISED-PITCHES.json"
    prior_drafts = {d["domain"]: d for d in read(revised_path)["drafts"]} if revised_path.exists() else {}
    openings = {
        "amtc.ae": ("I saw AMTC's announcement that a new website is in progress.", "service and equipment-information", "Staff-managed website content for AMTC"),
        "ablaqco.com": ("I saw Ablaq's announcement that a new website is coming for the studio.", "studio and project-information", "Staff-managed website content for Ablaq"),
        "arabiandream.com": ("I saw The Arabian Dream's announcement that a new website is coming.", "relocation and company-formation service", "Staff-managed website content for The Arabian Dream"),
    }
    for original in held["drafts"]:
        prior = prior_drafts.get(original["domain"])
        if prior and prior.get("status") in {"sent", "sending", "unknown"}:
            drafts.append(prior)
            continue
        first, pages, subject = openings[original["domain"]]
        body = (
            f"Hello {original['organisation'].split(' / ')[0]} team,\n\n{first}\n\n"
            f"I'm Ramish, founder of Nexora. Its website CMS lets staff manage branded pages, navigation and published content. "
            f"A focused evaluation could use three sample {pages} pages and one editor to check draft, preview and publication before agreeing a wider website scope.\n\n"
            "I've attached a 90-second narrated introduction to Nexora's wider membership platform. A website-editor walkthrough would focus on your content needs.\n\n"
            f"Would a 15-minute website-editor walkthrough be useful for your {pages} pages?\n\n"
            "Best,\nRamish Qureshi\nFounder, Nexora\nramishqureshi99@gmail.com\n\n"
            "Advertisement: Nexora website/CMS software.\nEOBI Building, I-8 Markaz, Islamabad 44790, Pakistan.\n"
            'To stop all marketing emails from Nexora, reply "unsubscribe" or "no thanks" and we won\'t contact you again.'
        )
        draft = dict(original)
        draft.update({"subject": subject, "text": body, "html": "<div>" + "".join("<p>" + html.escape(p).replace("\n", "<br>") + "</p>" for p in body.split("\n\n")) + "</div>", "copyRevisedAt": STAMP, "status": "held", "sendEligible": False, "originalDraftSource": str((source.parent / "held-drafts.json").relative_to(ROOT)), "copyVersion": 2})
        drafts.append(draft)
    write(OUT / "REVISED-PITCHES.json", {"createdAt": STAMP, "notGmailDrafts": True, "sendReadyCount": 0, "drafts": drafts})
    (OUT / "REVISED-PITCHES.md").write_text("# Revised Middle East pitches\n\nConfirmed sends are preserved. Held drafts require revalidation before sending. This utility never sends email.\n\n" + "\n\n---\n\n".join(f"## {d['organisation']}\n\nStatus: {d['status']}\n\nTo: {d['to']}\n\nSubject: {d['subject']}\n\n{d['text']}\n\n" + ("Confirmed Gmail message: " + d["messageId"] if d["status"] == "sent" else "Held because: " + "; ".join(d.get("holds", []))) for d in drafts) + "\n", encoding="utf-8")
    write(OUT / "BASELINE-METRICS.json", {"generatedAt": STAMP, "historicalUniqueOrganisationsInLedger": len(contacted), "currentResearchLeads": len(queue), "currentSendEligible": 0, "revisedHeldPitches": len(drafts), "recordedPositiveResponses": len(state.get("positiveResponses", [])), "discoveryCallsHeld": None, "qualifiedOpportunities": None, "proposalsSent": None, "paidClients": None, "unknownExplanation": "No evidence in campaign records; null means unverified, not zero", "confirmedNewSendsFromThisReview": 0, "attachmentVerified": True})
    print(json.dumps({"pipeline": str(path), "researchLeads": len(queue), "heldPitches": len(drafts), "sendEligible": 0, "sent": 0}))


if __name__ == "__main__":
    main()
