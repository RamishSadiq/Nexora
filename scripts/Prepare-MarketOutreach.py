"""Prepare personalised messages for the agreed UK association list; never send."""
from datetime import datetime, timezone
from email.message import EmailMessage
from email import policy
from email.parser import BytesParser
from html import escape
import json
from pathlib import Path
import re

root = Path(__file__).resolve().parent.parent
market = root / "docs" / "market"
folder = market / "outreach" / "2026-10-08"
folder.mkdir(parents=True, exist_ok=True)
sender = "ramishqureshi99@gmail.com"
signature = f"Best,\nRamish Qureshi\nFounder, Nexora\n{sender}"
footer = "I found this contact on your association's public website. If this isn't relevant, reply 'no thanks' and I won't follow up."
video = market / "pitch" / "nexora-product-demo-male.mp4"
video_bytes = video.read_bytes()
product = "We've built Nexora, a membership management platform for associations. It brings company and contact records, membership applications, staff review and member self-service into one connected system, with the aim of reducing repeated updates and making each member's information easier to follow."
ai = "Nexora also supports an AI assistant inside the CRM workspace. When enabled, your team can access AI assistance alongside their day-to-day membership work."
demo = "I've attached a 90-second narrated demonstration of Nexora. It also has on-screen captions, so you can watch it with or without sound."
metadata = {
    "GTMA": ("https://www.gtma.co.uk/contact/", "00375508"),
    "Association of Fencing Industries": ("https://afiorg.uk/about-us/contact-us/", "10421013"),
    "BASA": ("https://members.basa.uk.com/contact", None),
    "BAFSA": ("https://www.bafsa.org.uk/contact-us", "01194637"),
    "British Promotional Merchandise Association": ("https://bpma.co.uk/contact/", "04546290"),
}
messages = []
templates = (market / "FIRST-FIVE-EMAILS.md").read_text(encoding="utf-8")
for section in re.split(r"(?m)^## ", templates)[1:]:
    header, rest = section.split("\n", 1)
    organisation, recipient = header.strip().split(" — ")
    subject_match = re.search(r"(?m)^Subject: (.+)$", rest)
    body = rest[subject_match.end():].strip()
    body = re.sub(r"\n\[actual business identity \| website \| reply email\]", "", body)
    body = body.replace("Best,\nRamish\nFounder, Nexora", signature)
    body = body.replace("If this isn't relevant, let me know and I won't follow up.", footer)
    source, number = metadata[organisation]
    messages.append({"organisation": organisation, "kind": "buyer", "to": recipient,
        "subject": subject_match.group(1), "text": body, "contactSource": source,
        "companyNumber": number, "status": "held" if organisation == "BASA" else "ready",
        "holdReason": "Unincorporated association; user confirmed no prior permission on 2026-10-08" if organisation == "BASA" else None})

additional = [
    ("Commercial Interiors UK", "info@commercial-interiorsuk.com", "Commercial Interiors UK team", "I saw that your membership process combines an application with a Code of Practice. I'd be interested to understand how application review connects with your company and contact records.", "Would your membership or operations lead be open to a 15-minute conversation about whether Nexora could improve a specific part of Commercial Interiors UK's membership process?", "https://www.commercial-interiorsuk.com/application-form", "01298682"),
    ("British Constructional Steelwork Association", "lorraine.mackinder@bcsa.org.uk", "Lorraine", "I saw that you handle BCSA's membership and new-member administration. I'm contacting you about the connection between member-company records, their contacts and the application process.", "Would you be open to a 15-minute conversation about whether Nexora could help with a specific part of BCSA's membership administration?", "https://www.bcsa.org.uk/contact-us/", "00895252"),
    ("British Coatings Federation", "info@bcf.co.uk", "BCF team", "BCF brings together businesses across coatings, inks and wallcoverings. I'd be interested to understand how your team keeps member-company records and their contacts connected with membership administration.", "Would your membership or operations lead be open to a 15-minute conversation to see whether Nexora could be useful for BCF?", "https://coatings.org.uk/", "00745398"),
    ("Society of Independent Brewers and Associates", "membership@siba.co.uk", "SIBA membership team", "I saw that SIBA supports both breweries and supplier associates and already offers a member toolbox. I'd be interested to understand how those member services connect with the records your team maintains.", "Would you be open to a 15-minute conversation about whether Nexora could improve a specific part of SIBA's membership process?", "https://siba.co.uk/wp-content/uploads/2026/05/SIBA-Privacy-Notice-March-2026.pdf", "03163350"),
    ("British Independent Retailers Association", "membership@bira.co.uk", "Bira membership team", "I saw that Bira provides independent retailers with a member area and a range of services. I'd be interested to understand how member information stays aligned across your team's day-to-day work.", "Would your membership or operations lead be open to a 15-minute conversation about whether Nexora could help with a specific part of Bira's member administration?", "https://bira.co.uk/contact-us/", "11628600"),
]
for organisation, recipient, greeting, opening, invitation, source, number in additional:
    messages.append({"organisation": organisation, "kind": "buyer", "to": recipient,
        "subject": f"A short Nexora demo for {organisation}'s membership team",
        "text": "\n\n".join([f"Hi {greeting},", opening, product, ai, demo,
            "We're inviting a small number of associations to explore a focused evaluation. " + invitation, signature, footer]),
        "contactSource": source, "companyNumber": number, "status": "ready", "holdReason": None})

referrals = [
    ("Trade Association Forum", "secretariat@taforum.org", "TAF team", "TAF's work with trade associations across different sectors looks like a relevant place to start conversations about membership operations.", "Would you be open to a 15-minute conversation about whether there's an appropriate way to introduce Nexora to relevant association teams in your network?", "https://www.taforum.org/", "08849164"),
    ("British Cleaning Council", "info@britishcleaningcouncil.org", "British Cleaning Council team", "I saw that the British Cleaning Council brings together trade and membership associations across cleaning, hygiene and waste. I'm looking to connect with teams that want to explore their membership administration process.", "Would you be open to a short conversation about whether Nexora could be relevant to any of your member associations, and the best way to introduce it?", "https://britishcleaningcouncil.org/", "02128175"),
]
for organisation, recipient, greeting, opening, invitation, source, number in referrals:
    messages.append({"organisation": organisation, "kind": "referral", "to": recipient,
        "subject": f"Introducing Nexora to {organisation}'s association network",
        "text": "\n\n".join([f"Hi {greeting},", opening, product, ai, demo,
            "We're inviting a small number of associations to explore a focused evaluation. " + invitation, signature, footer]),
        "contactSource": source, "companyNumber": number, "status": "ready", "holdReason": None})

for item in messages:
    assert not re.search(r"\[[^\]]+\]", item["text"]), "Unresolved placeholder"
    assert item["to"] != sender, "Recipient is the earlier test inbox"
    item["companySource"] = "https://find-and-update.company-information.service.gov.uk/company/" + item["companyNumber"] if item["companyNumber"] else "https://www.basa.uk.com/about-basa/"
    item["html"] = "<!doctype html><html><body>" + "".join("<p>" + escape(p).replace("\n", "<br>") + "</p>" for p in item["text"].split("\n\n")) + "</body></html>"
    mail = EmailMessage(policy=policy.SMTP)
    mail["From"] = f"Ramish Qureshi <{sender}>"
    mail["To"] = item["to"]
    mail["Subject"] = item["subject"]
    mail["X-Unsent"] = "1"
    mail.set_content(item["text"])
    mail.add_alternative(item["html"], subtype="html")
    mail.add_attachment(video_bytes, maintype="video", subtype="mp4", filename=video.name)
    filename = re.sub(r"[^a-z0-9]+", "-", item["organisation"].lower()).strip("-") + ".eml"
    mail_path = folder / filename
    mail_path.write_bytes(mail.as_bytes())
    parsed = BytesParser(policy=policy.default).parsebytes(mail_path.read_bytes())
    assert str(parsed["To"]) == item["to"]
    assert list(parsed.iter_attachments())[0].get_payload(decode=True) == video_bytes
    item["preparedEmail"] = str(mail_path)

assert len(messages) == 12 and len({m["to"] for m in messages}) == 12
campaign = {"preparedAt": datetime.now(timezone.utc).isoformat(), "sender": sender,
    "attachment": str(video), "attachmentBytes": len(video_bytes), "messages": messages,
    "duplicateCheck": "Gmail Nexora search found only the test message; no prior prospect outreach or opt-outs found",
    "deliveryMeaning": "Sent means Gmail accepted the message; recipient receipt is not established by send acceptance"}
campaign_path = folder / "campaign.json"
if campaign_path.exists():
    existing = json.loads(campaign_path.read_text(encoding="utf-8"))
    if any(m.get("status") in ('sent', 'sending', 'unknown', 'failed') for m in existing.get("messages", [])):
        raise RuntimeError("Existing sent ledger must be preserved; do not regenerate over it")
campaign_path.write_text(json.dumps(campaign, indent=2), encoding="utf-8")
print(json.dumps(campaign))
