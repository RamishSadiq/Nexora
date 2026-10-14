"""Verify source copies, primary maps, secrets exclusion and document packages."""
from pathlib import Path
import hashlib
import html
import importlib.util
import json
import re
import sys
import zipfile
from urllib.parse import unquote

sys.stdout.reconfigure(encoding='utf-8')
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'docs/beginner-guides';QA=ROOT/'artifacts/platform-guide-qa'
sys.path.insert(0,str(Path(__file__).parent))
spec=importlib.util.spec_from_file_location('platform_builder',Path(__file__).with_name('Build-PlatformGuides.py'))
b=importlib.util.module_from_spec(spec);spec.loader.exec_module(b)
page=(OUT/(b.MASTER+'.html')).read_text(encoding='utf-8')
sources=json.loads(re.search(r'<script id="source-data" type="application/json">(.*?)</script>',page,re.S)[1])
manifest=json.loads((OUT/'source-manifest.json').read_text(encoding='utf-8'))
assert len(sources)==manifest['file_count']
assert sum(s['lines'] for s in sources)==manifest['source_lines']
assert [{k:v for k,v in s.items() if k!='text'} for s in sources]==manifest['files']
by_path,routes,modules,contexts=b.audit(sources)
assert len(routes)==manifest['route_entries'] and modules==manifest['modules']
assert [list(x) for x in contexts]==manifest['contexts']
assert len({s['id'] for s in sources})==len(sources)
assert all(s['chapters'] and set(s['chapters'])<=set(range(1,len(b.GUIDES)+1)) for s in sources)
primary_checks=0
for g in b.GUIDES:
    if g['number']==1:continue
    md=(OUT/'chapters'/f"{g['number']:02d} {g['title']}.md").read_text(encoding='utf-8')
    for key in ['frontend','backend','model']:
        s=by_path[g[key]]
        numbered='\n'.join(f'{i:04d}  {line}' for i,line in enumerate(s['text'].splitlines(),1))
        assert numbered in md,(g['number'],key)
        assert s['sha256'] in md
        primary_checks+=1
drift=[]
for s in sources:
    live=b.origin_path(s)
    assert live.is_file(),s['path']
    if hashlib.sha256(live.read_bytes()).hexdigest()!=s['sha256']:drift.append(s['path'])
    elif not s['redacted']:assert live.read_bytes().decode('utf-8-sig')==s['text'],s['path']
    if s.get('project')=='company-dash':assert not re.search(r'(^|/)(data|artifacts|node_modules|config\.local\.json|\.env[^/]*)(/|$)',s['path'])
launch=json.loads((ROOT/'.vscode/launch.json').read_text(encoding='utf-8-sig'))
secrets=[v for c in launch['configurations'] for k,v in c.get('env',{}).items() if k.lower().endswith('password') and isinstance(v,str) and len(v)>4 and v.lower() not in ['true','false']]
docs=[p for p in OUT.rglob('*.docx') if 'history' not in p.relative_to(OUT).parts]
assert len(docs)==len(b.GUIDES)+1,len(docs)
doc_hashes={}
for p in docs:
    with zipfile.ZipFile(p) as z:
        assert z.testzip() is None,p.name
        for name in z.namelist():
            if name.endswith('.xml'):
                xml=html.unescape(z.read(name).decode('utf-8'))
                assert not any(v in xml for v in secrets),p.name
    doc_hashes[p.stem]=hashlib.sha256(p.read_bytes()).hexdigest()
links=0
for p in OUT.rglob('*.md'):
    if 'history' in p.relative_to(OUT).parts:continue
    text=p.read_text(encoding='utf-8')
    assert not any(v in text for v in secrets),p.name
    text=re.sub(r'```[^\n]*\n.*?```','',text,flags=re.S)
    for ref in re.findall(r'\]\((<[^>]+>|[^)\n]+)\)',text):
        ref=unquote(ref.strip('<>'))
        if ref.startswith(('#','http:','https:','vscode:')):continue
        ref=re.sub(r':\d+$','',ref)
        target=Path(ref) if re.match(r'^[A-Za-z]:[/\\]',ref) else p.parent/ref
        assert target.exists(),(p.name,ref)
        links+=1
assert not any(v in html.unescape(page) for v in secrets)
history=list((OUT/'history').glob('*.zip'))
assert history,'Previous complete guide must be preserved'
for p in history:
    with zipfile.ZipFile(p) as z:assert z.testzip() is None,p.name
result=dict(status='passed',snapshot=manifest['captured_at_utc'],guides=len(b.GUIDES),docx_files=len(docs),source_files=len(sources),
            company_dash_files=sum(s.get('project')=='company-dash' for s in sources),source_lines=manifest['source_lines'],
            complete_primary_copies_checked=primary_checks,local_links_checked=links,configured_password_omitted=True,
            history_archives_checked=len(history),current_source_drift=drift,docx_sha256=doc_hashes,application_regression_run=False)
QA.mkdir(parents=True,exist_ok=True);(QA/'structural-evidence.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='docx_sha256'},indent=2))
