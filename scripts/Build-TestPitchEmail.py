"""Build an unsent email preview with the real pitch video attached.

No network calls or sending credentials. From is intentionally left unset so
an email client can use the sending account chosen by the user.
"""

from email.message import EmailMessage
from email import policy
from html import escape
from pathlib import Path

root = Path(__file__).resolve().parent.parent
market = root / "docs" / "market"
video = market / "pitch" / "nexora-product-demo-male.mp4"
output = market / "nexora-test-pitch.eml"

paragraphs = [
    "Hi Ramish,",
    "Keeping member records, applications and day-to-day admin in sync can take more time than it should.",
    "We've built Nexora, a membership management platform for associations. It brings company and contact records, membership applications, staff review and member self-service into one connected system, with the aim of reducing repeated updates and making each member's information easier to follow.",
    "Nexora also supports an AI assistant inside the CRM workspace. When enabled, your team can access AI assistance alongside their day-to-day membership work.",
    "I've attached a 90-second narrated demonstration of Nexora. It also has on-screen captions, so you can watch it with or without sound.",
    "We're now inviting a small number of associations to explore a focused evaluation around their own membership process. Would you be open to a 15-minute conversation to see whether Nexora could be useful for your association?",
    "Best,\nRamish\nFounder, Nexora",
    "If this isn't relevant, let me know and I won't follow up.",
]

message = EmailMessage(policy=policy.SMTP)
message["To"] = "ramishqureshi99@gmail.com"
message["Subject"] = "Nexora: membership management and AI access for your association"
message["X-Unsent"] = "1"
message.set_content("\n\n".join(paragraphs) + "\n")
html = "<!doctype html><html><body>" + "".join(
    "<p>" + escape(paragraph).replace("\n", "<br>") + "</p>" for paragraph in paragraphs
) + "</body></html>"
message.add_alternative(html, subtype="html")
message.add_attachment(video.read_bytes(), maintype="video", subtype="mp4", filename=video.name)
output.write_bytes(message.as_bytes())

# Verify the actual packaged message, including byte-for-byte attachment integrity.
from email.parser import BytesParser
parsed = BytesParser(policy=policy.default).parsebytes(output.read_bytes())
attachments = list(parsed.iter_attachments())
assert str(parsed["To"]) == "ramishqureshi99@gmail.com"
assert parsed["From"] is None
assert len(attachments) == 1
assert attachments[0].get_filename() == video.name
assert attachments[0].get_payload(decode=True) == video.read_bytes()
assert parsed.get_body(preferencelist=("plain",)) is not None
assert parsed.get_body(preferencelist=("html",)) is not None
(market / "nexora-test-pitch-preview.html").write_text(html, encoding="utf-8")
print(f"Prepared unsent email: {output} ({output.stat().st_size:,} bytes)")
print("Recipient and video attachment verified. No email has been sent.")
