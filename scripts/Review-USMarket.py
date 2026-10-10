"""Summarise evidence for manual prospect qualification; no email is sent."""
import json
from pathlib import Path
import re
from urllib.parse import urlparse, unquote

folder = Path(__file__).resolve().parent.parent / 'docs/market/us'
items = json.loads((folder / 'research-evidence.json').read_text(encoding='utf-8'))
contact_file = folder / 'contact-evidence.json'
contacts = {i['name']:i['pages'] for i in json.loads(contact_file.read_text(encoding='utf-8'))} if contact_file.exists() else {}
summaries = []
for index, item in enumerate(items, 1):
    own = (urlparse(item['url']).hostname or '').replace('www.', '')
    emails = []
    contact_links = []
    snippets = []
    for page in item['pages'] + contacts.get(item['name'], []):
        if page['status'] != 'read': continue
        source = page.get('finalUrl', page['url'])
        for link in page.get('links', []):
            url = link['url']; label = link['label']
            if url.lower().startswith('mailto:'):
                email = unquote(unquote(url[7:].split('?')[0])).strip()
            elif '/cdn-cgi/l/email-protection#' in url:
                try:
                    raw = bytes.fromhex(url.split('#')[-1]); email = ''.join(chr(b ^ raw[0]) for b in raw[1:])
                except Exception: continue
            else: email = None
            if email: email = unquote(email).strip()
            if email and re.fullmatch(r'[^\s@]+@[^\s@]+\.[A-Za-z]{2,}', email):
                if email.lower().endswith('@' + own):
                    rec = {'email':email,'source':source,'label':label}
                    if not any(e['email'].lower()==email.lower() for e in emails): emails.append(rec)
            if re.search(r'contact|staff|team', label, re.I) and url.startswith('http') and own in (urlparse(url).hostname or ''):
                if url not in contact_links: contact_links.append(url)
        text = re.sub(r'\s+', ' ', page.get('text', ''))
        matches = list(re.finditer(r'member compan|membership is open|join.{0,50}compan|compan.{0,70}member|member.{0,70}compan|membership categor|business membership', text, re.I))
        for match in matches[:3]:
            snippet = text[max(0,match.start()-65):min(len(text),match.end()+190)]
            if not any(s['text']==snippet for s in snippets): snippets.append({'source':source,'text':snippet})
    summaries.append({'id':index,'name':item['name'],'url':item['url'],'sector':item['sector'], 'status':item['status'],'emails':emails[:12],'contactLinks':contact_links[:5],'membershipEvidence':snippets[:4]})
(folder / 'research-summary.json').write_text(json.dumps(summaries,indent=2),encoding='utf-8')
print(json.dumps(summaries))
