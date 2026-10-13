"""Refresh Word layout from existing teaching snapshots without recapturing code."""
from pathlib import Path
import sys
from guide_docx import build
sys.stdout.reconfigure(encoding='utf-8')
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'docs/beginner-guides'
master=OUT/'Nexora complete beginner guide.md'
build(master.read_text(encoding='utf-8-sig'),master.with_suffix('.docx'),OUT,master.stem)
for path in sorted((OUT/'chapters').glob('*.md')):
    content=path.read_text(encoding='utf-8-sig').split('## Complete primary source files')[0]
    content=content.replace('](../assets/','](assets/')
    build(content,path.with_suffix('.docx'),OUT,content.splitlines()[0].removeprefix('# '))
print('Refreshed master and 29 individual Word guides from existing snapshots')
