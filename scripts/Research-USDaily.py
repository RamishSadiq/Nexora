"""Reverify a daily list of public association URLs. Never sends emails."""
import argparse
from concurrent.futures import ThreadPoolExecutor, as_completed
import importlib.util
import json
from pathlib import Path
import re

parser = argparse.ArgumentParser()
parser.add_argument('--date', required=True)
args = parser.parse_args()
if not re.fullmatch(r'\d{4}-\d{2}-\d{2}', args.date):
    raise ValueError('Expected YYYY-MM-DD')
root = Path(__file__).resolve().parent.parent
folder = root / 'docs/market/us/outreach' / args.date
plan = json.loads((folder / 'research-plan.json').read_text(encoding='utf-8'))
spec = importlib.util.spec_from_file_location('research', root / 'scripts/Research-USMarket.py')
research = importlib.util.module_from_spec(spec)
spec.loader.exec_module(research)

def verify(item):
    return {**item, 'pages':[research.fetch(url) for url in item['urls']]}

done = {}
with ThreadPoolExecutor(max_workers=5) as pool:
    for future in as_completed([pool.submit(verify,item) for item in plan]):
        item = future.result()
        done[item['candidateId']] = item
        saved = [done[item['candidateId']] for item in plan if item['candidateId'] in done]
        (folder / 'research-evidence.json').write_text(json.dumps(saved,indent=2),encoding='utf-8')
        print(json.dumps({'candidateId':item['candidateId'], 'name':item['name'],
                         'read':sum(page['status']=='read' for page in item['pages']),
                         'total':len(item['pages']), 'completed':len(done)}),flush=True)
