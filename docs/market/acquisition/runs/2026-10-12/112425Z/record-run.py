"""Record this held run using the verified market-recording helper; never sends email."""
from pathlib import Path

source_path = Path(__file__).resolve().parent.parent / '062819Z/record-run.py'
source = source_path.read_text(encoding='utf-8')
for before, after in [
    ("'062819Z'", "'112425Z'"),
    ("'us-rega'", "'us-fitness-showrooms'"),
    ("'uae-sharpminds'", "'uae-moma'"),
    ('2026-10-12T07:28:19.133+01:00', '2026-10-12T12:24:25.827+01:00'),
    ('Held body only; published recipient not verified', 'Held body only; recipient-specific UAE lawful route unresolved, To deliberately not selected'),
    ('Initial and completion Gmail checks have recovered; no new positive response. Send-approval block remains unchanged.', 'Initial and completion Gmail checks complete; no new positive response. Sender restriction unchanged; no notification needed.'),
    ("state['lastGmailReadRecoveryAt'] = data['evidence']['completion']['at']\n", ''),
    ('MEHeldWithoutRecipient', 'MEHeldForLawfulRoute'),
]:
    assert before in source, 'Recording helper structure changed: ' + before
    source = source.replace(before, after)

start = source.index("(run_dir / 'run-report.md').write_text(")
end = source.index('\nassert all(hashlib.sha256(Path(p)', start)
replacement = """(run_dir / 'run-report.md').write_text(
    '# Dual-market run: 12 October 2026\\n\\n' + local.strftime('%H:%M') + ' Europe/London. Zero emails sent; zero send calls.\\n\\n'
    'Initial and completion Gmail checks succeeded: paginated domain/overlap/spam/trash/delivery searches, actual inbound bodies, 55 associated conversations and Sent recipient/subject/video metadata. No new positive reply, opt-out or delivery failure was found. Known suppression scopes preserved.\\n\\n'
    'US: Fitness Showrooms - official planned 2026 storefront and public showroom-content workflow; exact general team inbox verified. Local MIME pitch includes plain text, HTML and the verified MP4, held for sender approval. Existing website maintenance provider is recorded; no dissatisfaction or buying requirement inferred.\\n\\n'
    'UAE: MOMA International - public architecture/interiors/landscape content and named Dubai business contact verified. Local body pitch held because recipient-specific lawful email route is unresolved; no To selected or MIME prepared. Official UAE legislation fetch returned 403; no generic-mailbox exemption or blanket invitation requirement invented.\\n\\n'
    'Both slots unused. All 55 confirmed sends, four manifests, historical cadence records and global suppressions preserved. Weekly review next due 17 October. Dedicated CMS/portal demonstrations and public product page remain outstanding. Hudson Valley Web Girl added only to partner research, without any claimed founder relationship, willingness or message. Existing holds not repeatedly revisited.\\n',
    encoding='utf-8')"""
source = source[:start] + replacement + source[end:]
exec(compile(source, str(Path(__file__).resolve()), 'exec'))
