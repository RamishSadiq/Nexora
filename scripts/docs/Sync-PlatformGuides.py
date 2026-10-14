"""Detect documentation drift and archive/rebuild the maintained collection.

Reads both application repositories. Writes documentation only in Nexora.
No product services, agent runs, business operations or Git mutations occur.
"""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import subprocess
import sys
import zipfile
from datetime import datetime, timezone

sys.stdout.reconfigure(encoding='utf-8')
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'docs/beginner-guides'
QA=ROOT/'artifacts/platform-guide-qa'
sys.path.insert(0,str(Path(__file__).parent))
spec=importlib.util.spec_from_file_location('platform_builder',Path(__file__).with_name('Build-PlatformGuides.py'))
builder=importlib.util.module_from_spec(spec);spec.loader.exec_module(builder)

def drift():
    manifest=json.loads((OUT/'source-manifest.json').read_text(encoding='utf-8'))
    sources,_=builder.capture()
    before={s['path']:s for s in manifest['files']}
    after={s['path']:s for s in sources}
    changed=[p for p in sorted(before.keys()&after.keys()) if before[p]['sha256']!=after[p]['sha256']]
    added=sorted(after.keys()-before.keys());removed=sorted(before.keys()-after.keys())
    editorial=manifest.get('editorial_fingerprints')!=builder.editorial_fingerprints()
    state_path=OUT/'maintenance-state.json'
    review_state=json.loads(state_path.read_text(encoding='utf-8')).get('status') if state_path.is_file() else 'unrecorded'
    return dict(status='changed' if changed or added or removed or editorial else 'pending_review' if review_state=='pending_review' else 'up_to_date',
                review_state=review_state,
                snapshot=manifest['captured_at_utc'],changed=changed,added=added,removed=removed,
                editorial_changed=editorial,affected_guides=sorted({n for p in changed+added+removed for n in (after.get(p) or before[p])['chapters']})),sources

def archive(manifest):
    history=OUT/'history';history.mkdir(exist_ok=True)
    stamp=datetime.fromisoformat(manifest['captured_at_utc']).strftime('%Y-%m-%dT%H%M%SZ')
    digest=hashlib.sha256((OUT/(builder.MASTER+'.html')).read_bytes()).hexdigest()[:10]
    target=history/f'{stamp}-{digest}.zip'
    if not target.exists():
        selected=[OUT/(builder.MASTER+ext) for ext in ['.html','.docx','.md']]
        selected += [OUT/name for name in ['source-manifest.json','coverage.json','README.md','DOCUMENTATION-MAINTENANCE.md'] if (OUT/name).is_file()]
        selected += list((OUT/'assets').rglob('*.png'))
        with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
            for p in selected:z.write(p,p.relative_to(OUT).as_posix())
    with zipfile.ZipFile(target) as z:
        if z.testzip() is not None:raise ValueError('History archive failed verification')
    return target

def refresh(sources):
    builder.audit(sources) # Fail before touching the maintained manual if an anchor is stale.
    manifest=json.loads((OUT/'source-manifest.json').read_text(encoding='utf-8'))
    dated=archive(manifest)
    QA.mkdir(parents=True,exist_ok=True)
    backup=QA/'collection-before-refresh.zip'
    original=[p for p in OUT.rglob('*') if p.is_file() and 'history' not in p.relative_to(OUT).parts]
    with zipfile.ZipFile(backup,'w',zipfile.ZIP_DEFLATED) as z:
        for p in original:z.write(p,p.relative_to(OUT).as_posix())
    try:
        subprocess.run([sys.executable,str(Path(__file__).with_name('Build-PlatformGuides.py'))],cwd=ROOT,check=True)
    except BaseException:
        # Restore only the documentation collection; never delete or reset application source.
        original_names={p.relative_to(OUT).as_posix() for p in original}
        for p in OUT.rglob('*'):
            if p.is_file() and 'history' not in p.relative_to(OUT).parts and p.relative_to(OUT).as_posix() not in original_names:
                if not p.resolve().is_relative_to(OUT.resolve()):raise ValueError('Unexpected documentation path')
                p.unlink()
        with zipfile.ZipFile(backup) as z:
            for name in z.namelist():
                p=OUT/name
                if not p.resolve().is_relative_to(OUT.resolve()):raise ValueError('Invalid backup member')
                p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(z.read(name))
        raise
    current=json.loads((OUT/'source-manifest.json').read_text(encoding='utf-8'))
    state=dict(status='pending_review',snapshot=current['captured_at_utc'],previous_copy=dated.relative_to(ROOT).as_posix(),
               refreshed_at_utc=datetime.now(timezone.utc).isoformat(timespec='seconds'),
               note='Run browser/source checks and render/review the Word pages before marking reviewed.')
    (OUT/'maintenance-state.json').write_text(json.dumps(state,indent=2)+'\n',encoding='utf-8')
    return state

def reviewed():
    manifest=json.loads((OUT/'source-manifest.json').read_text(encoding='utf-8'))
    browser=json.loads((QA/'browser-evidence.json').read_text(encoding='utf-8'))
    assert browser['status']=='passed' and browser['chapters']==manifest['guide_count'] and browser['full_source_files']==manifest['file_count'] and browser['snapshot']==manifest['captured_at_utc']
    report=json.loads((QA/'render-evidence.json').read_text(encoding='utf-8'))
    assert len(report)==manifest['guide_count'] and all(not r['small_pages'] and r['overflow_chars']==0 for r in report)
    structural=json.loads((QA/'structural-evidence.json').read_text(encoding='utf-8'))
    assert structural['status']=='passed' and structural['snapshot']==manifest['captured_at_utc']
    assert all(r['docx_sha256']==structural['docx_sha256'][r['document']]==hashlib.sha256((OUT/('' if r['document']==builder.MASTER else 'chapters')/(r['document']+'.docx')).read_bytes()).hexdigest() for r in report)
    state=json.loads((OUT/'maintenance-state.json').read_text(encoding='utf-8'))
    assert state['snapshot']==manifest['captured_at_utc']
    state.update(status='reviewed',reviewed_at_utc=datetime.now(timezone.utc).isoformat(timespec='seconds'),
                 note='Source/browser checks passed; Word pages were rendered and visually reviewed. Product regressions were not rerun.')
    (OUT/'maintenance-state.json').write_text(json.dumps(state,indent=2)+'\n',encoding='utf-8')
    return state

def main():
    parser=argparse.ArgumentParser()
    mode=parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--check',action='store_true');mode.add_argument('--refresh',action='store_true');mode.add_argument('--reviewed',action='store_true')
    parser.add_argument('--force',action='store_true',help='Refresh even when no source drift exists.')
    args=parser.parse_args()
    if args.reviewed:result=reviewed()
    else:
        result,sources=drift()
        if args.refresh and (result['status']=='changed' or args.force):result=refresh(sources)
    print(json.dumps(result,indent=2))

if __name__=='__main__':main()
