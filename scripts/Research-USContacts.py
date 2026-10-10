"""Read public association staff/contact pages for shortlisted prospects only."""
from concurrent.futures import ThreadPoolExecutor, as_completed
import json
from pathlib import Path
from urllib.parse import urlparse
from importlib.util import spec_from_file_location, module_from_spec

root = Path(__file__).resolve().parent.parent
spec = spec_from_file_location('us_research', root / 'scripts/Research-USMarket.py')
module = module_from_spec(spec); spec.loader.exec_module(module)
folder = root / 'docs/market/us'
ids = set(json.loads((folder / 'SHORTLIST-IDS.json').read_text()))
items = json.loads((folder / 'research-summary.json').read_text())
selected = [i for i in items if i['id'] in ids]

def read_contacts(item):
    urls = [u for u in item['contactLinks'] if len(u)<300][:2]
    if not urls: urls = [item['url'].rstrip('/') + '/contact-us/']
    return {'id':item['id'],'name':item['name'],'pages':[module.fetch(u) for u in urls]}

out = {}
with ThreadPoolExecutor(max_workers=5) as pool:
    for future in as_completed([pool.submit(read_contacts,i) for i in selected]):
        item = future.result(); out[item['name']] = item
        (folder / 'contact-evidence.json').write_text(json.dumps([out[i['name']] for i in selected if i['name'] in out],indent=2),encoding='utf-8')
        print(json.dumps({'completed':len(out),'total':len(selected),'name':item['name'],'read':sum(p['status']=='read' for p in item['pages'])}),flush=True)
