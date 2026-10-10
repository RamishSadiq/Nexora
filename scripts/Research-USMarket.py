"""Read official association pages for a bounded, evidence-linked market screen.

No messages are sent. Candidate names and URL guesses are unverified until read.
"""
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
from html.parser import HTMLParser
import json
from pathlib import Path
import re
import urllib.request
from urllib.parse import urljoin, urlparse

ROOT = Path(__file__).resolve().parent.parent
FOLDER = ROOT / 'docs/market/us'

class Page(HTMLParser):
    def __init__(self):
        super().__init__(); self.text = []; self.links = []; self.skip = 0
        self.href = None; self.label = []
    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag in ('script', 'style', 'noscript'): self.skip += 1
        if tag == 'a': self.href = attrs.get('href'); self.label = []
    def handle_endtag(self, tag):
        if tag in ('script', 'style', 'noscript'): self.skip = max(0, self.skip - 1)
        if tag == 'a' and self.href:
            self.links.append((self.href, ' '.join(self.label))); self.href = None
        if tag in ('p', 'div', 'li', 'h1', 'h2', 'h3', 'section'): self.text.append('\n')
    def handle_data(self, data):
        if not self.skip:
            self.text.append(data)
            if self.href: self.label.append(data)

def fetch(url):
    started = datetime.now(timezone.utc).isoformat()
    try:
        req = urllib.request.Request(url, headers={'User-Agent':'Mozilla/5.0 (compatible; NexoraMarketResearch/1.0)'})
        with urllib.request.urlopen(req, timeout=15) as response:
            mime = response.headers.get('Content-Type', '')
            if 'html' not in mime: return {'url':url, 'status':'non-html', 'mime':mime, 'checkedAt':started}
            data = response.read(2_000_000).decode(response.headers.get_content_charset() or 'utf-8', errors='replace')
            final = response.geturl()
        p = Page(); p.feed(data)
        text = re.sub(r'[ \t]+', ' ', ''.join(p.text))
        text = re.sub(r'\n\s*\n+', '\n', text).strip()
        links = [{'url':urljoin(final, h), 'label':re.sub(r'\s+', ' ', t).strip()} for h,t in p.links]
        return {'url':url, 'finalUrl':final, 'status':'read', 'checkedAt':started, 'text':text[:60000], 'links':links}
    except Exception as exc:
        return {'url':url, 'status':'unavailable', 'checkedAt':started, 'error':str(exc)[:240]}

def research(item):
    homepage = fetch(item['url']); pages = [homepage]
    if homepage['status'] == 'read':
        domain = urlparse(homepage['finalUrl']).hostname.replace('www.', '')
        possible = []
        for link in homepage['links']:
            host = (urlparse(link['url']).hostname or '').replace('www.', '')
            if host != domain or '#' in link['url'] or link['url'].lower().endswith('.pdf'): continue
            label = link['label'].lower()
            if re.search(r'join|membership|become a member', label): possible.append((0,link['url']))
            elif re.search(r'contact|staff|team|about us|who we are', label): possible.append((1,link['url']))
        selected = []
        for _, url in sorted(possible):
            if url not in selected and url.rstrip('/') != homepage['finalUrl'].rstrip('/'): selected.append(url)
            if len(selected) >= 2: break
        pages.extend(fetch(url) for url in selected)
    item = dict(item); item['pages'] = pages
    item['status'] = 'official_pages_read' if homepage['status'] == 'read' else 'needs_web_verification'
    return item

if __name__ == '__main__':
    items = json.loads((FOLDER / 'US-CANDIDATES.json').read_text(encoding='utf-8'))
    assert len({i['name'] for i in items}) == len(items)
    out = {}
    with ThreadPoolExecutor(max_workers=5) as pool:
        jobs = {pool.submit(research, item):item['name'] for item in items}
        for job in as_completed(jobs):
            item = job.result(); out[item['name']] = item
            (FOLDER / 'research-evidence.json').write_text(json.dumps([out[i['name']] for i in items if i['name'] in out], indent=2), encoding='utf-8')
            print(json.dumps({'completed':len(out),'total':len(items),'name':item['name'],'status':item['status']}), flush=True)
