"""Print bounded source context for beginner documentation authoring."""
from pathlib import Path
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

ROOT = Path(__file__).resolve().parents[2]
for relative in sys.argv[1:]:
    path = ROOT / relative
    print('\nFILE', relative)
    if path.is_dir():
        for p in sorted(path.rglob('*.cs')):
            if 'Migrations' not in p.parts:
                for n, line in enumerate(p.read_text(encoding='utf-8-sig').splitlines(), 1):
                    matches = re.findall(r'Map(Get|Post|Put|Patch|Delete|Group)\(\s*"([^"\n]+)"', line)
                    if matches:
                        print(p.name, n, matches)
    else:
        for n, line in enumerate(path.read_text(encoding='utf-8-sig').splitlines(), 1):
            if n <= 24 or re.search(r'EntityHeading|<h[12]|return |Panel title|local|not configured|not available|unavailable|unconnected|AllowSelf|Connection|default', line):
                print(f'{n}: {line[:750]}')
