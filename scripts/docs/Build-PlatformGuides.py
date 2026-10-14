"""Build all Nexora beginner guides and the complete offline manual.

Reads production source without changing it. Word contains the teaching text and
short labelled extracts; HTML embeds every captured source file in full once.
Individual Markdown guides append their complete primary implementation files.
Uses the bundled Python environment with python-docx and Pillow.
"""
from pathlib import Path
import argparse
import base64
import hashlib
import html
import json
import os
import re
import subprocess
import sys
from datetime import datetime, timezone
from zoneinfo import ZoneInfo
from PIL import Image, ImageDraw, ImageFont
from platform_guide_catalog import GUIDES, owners
from guide_docx import build as build_docx

sys.stdout.reconfigure(encoding='utf-8')
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'docs/beginner-guides'
CHAPTERS=OUT/'chapters'
ASSETS=OUT/'assets/platform'
QA=ROOT/'artifacts/platform-guide-qa'
DATE=datetime.now(ZoneInfo('Europe/London')).strftime('%d %B %Y').lstrip('0')
MASTER='Nexora complete beginner guide'
COMPANY_DASH_ROOT=Path(os.environ.get('NEXORA_DOCS_COMPANY_DASH_ROOT', 'C:/Users/ramish/Documents/ChatGPT/Company Dash'))
SOURCE_EXT={'.tsx','.ts','.js','.html','.svg','.css','.cs','.csproj','.json','.http','.slnx','.props','.targets','.mjs','.ps1','.py','.yml','.yaml','.config','.md'}

def editorial_fingerprints():
    return {p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((ROOT/'scripts/docs').glob('*')) if p.is_file()}

def origin_path(source):
    return Path(source.get('absolute_path', (ROOT/source['path']).as_posix()))

def capture():
    paths=subprocess.check_output(['rg','--files','apps/web/src','backend/src','apps/web/tests','backend/tests'],cwd=ROOT,text=True).splitlines()
    extra=['package.json','apps/web/package.json','apps/web/next.config.ts','apps/web/tsconfig.json',
           'apps/web/playwright.config.ts','apps/web/eslint.config.mjs','backend/Nexora.slnx',
           'backend/Directory.Build.props','backend/Directory.Packages.props','.vscode/launch.json','.vscode/tasks.json',
           'scripts/Prepare-LocalDebug.ps1','scripts/Prepare-LocalDocumentScanner.ps1','scripts/Prepare-LocalTestData.mjs','scripts/Test-Nexora.ps1',
           'scripts/Test-Website.mjs','scripts/Test-Automations.mjs','scripts/Verify-AutomationCatalog.mjs',
           'scripts/Verify-LocalDebug.mjs','scripts/Verify-LaunchOrder.mjs','scripts/Verify-ConcurrentDebug.mjs',
           'scripts/Verify-PublicWebsite.mjs','scripts/Export-NexoraMigrations.ps1',
           '.github/workflows/ci.yml','global.json']
    paths=sorted(set(p.replace('\\','/') for p in paths+extra if (ROOT/p).is_file()))
    launch=json.loads((ROOT/'.vscode/launch.json').read_text(encoding='utf-8-sig'))
    passwords=[v for c in launch['configurations'] for k,v in c.get('env',{}).items() if k.lower().endswith('password') and isinstance(v,str) and v.lower() not in ['true','false'] and len(v)>4]
    result=[]
    for relative in paths:
        p=ROOT/relative
        if p.suffix not in SOURCE_EXT or any(x in p.parts for x in ['bin','obj','node_modules']):continue
        raw=p.read_bytes(); text=raw.decode('utf-8-sig'); redacted=False
        for secret in passwords:
            if secret in text:text=text.replace(secret,'<local password omitted>');redacted=True
        result.append(dict(id='S'+str(len(result)+1).zfill(4),path=relative,text=text,
                           sha256=hashlib.sha256(raw).hexdigest(),lines=len(text.splitlines()),
                           chapters=owners(relative),redacted=redacted,
                           absolute_path=p.as_posix(),project='nexora',
                           kind='test' if '/tests/' in relative else 'migration' if '/Migrations/' in relative else 'frontend' if relative.startswith('apps/web/src/') else 'backend' if relative.startswith('backend/src/') else 'configuration'))
    if not COMPANY_DASH_ROOT.is_dir():raise FileNotFoundError('Company Dash source is unavailable: '+str(COMPANY_DASH_ROOT))
    dash_paths=subprocess.check_output(['rg','--files','public','server','tests','docs'],cwd=COMPANY_DASH_ROOT,text=True).splitlines()
    dash_paths+=['package.json','config.example.json','README.md']
    for relative in sorted(set(p.replace('\\','/') for p in dash_paths)):
        p=COMPANY_DASH_ROOT/relative
        if not p.is_file() or p.is_symlink() or p.suffix not in SOURCE_EXT:continue
        if any(x in p.parts for x in ['node_modules','data','artifacts','.git']):continue
        raw=p.read_bytes();text=raw.decode('utf-8-sig');relative='company-dash/'+relative
        result.append(dict(id='S'+str(len(result)+1).zfill(4),path=relative,text=text,
                           sha256=hashlib.sha256(raw).hexdigest(),lines=len(text.splitlines()),
                           chapters=owners(relative),redacted=False,absolute_path=p.as_posix(),project='company-dash',
                           kind='frontend' if relative.startswith('company-dash/public/') else 'backend' if relative.startswith('company-dash/server/') else 'test' if relative.startswith('company-dash/tests/') else 'configuration'))
    return result,passwords

def audit(sources):
    bypath={s['path']:s for s in sources}; errors=[]
    for g in GUIDES:
        for key in ['frontend','backend','model']:
            if g[key] not in bypath:errors.append(f"{g['number']:02d} missing {key}: {g[key]}")
        path,anchor,after,_=g['exercise']
        if path not in bypath:errors.append(f"{g['number']:02d} missing exercise {path}")
        elif anchor not in bypath[path]['text']:errors.append(f"{g['number']:02d} missing anchor {anchor!r}")
    if errors:raise ValueError('\n'.join(errors))
    routes=[s for s in sources if s['path'].startswith('apps/web/src/app/') and Path(s['path']).name in ['page.tsx','route.ts','layout.tsx','error.tsx']]
    modules=sorted(p.name for p in (ROOT/'backend/src/Modules').iterdir() if p.is_dir())
    contexts=[]
    for s in sources:
        if s['kind']!='backend' or 'Migrations' in s['path']:continue
        for m in re.finditer(r'(?:public|internal)\s+(?:sealed\s+)?(?:partial\s+)?class\s+(\w+DbContext)\b',s['text']):
            if m[1]!='TenantDbContext':contexts.append((m[1],s['path']))
    # Partial Identity classes share one context; keep its main mapping file.
    unique_contexts={}
    for name,path in contexts:
        if name not in unique_contexts or Path(path).name==name+'.cs':unique_contexts[name]=path
    return bypath,routes,modules,sorted(unique_contexts.items())

def source_link(source, label=None, line=None):
    path=origin_path(source).as_posix()+(':'+str(line) if line else '')
    display=label or (source['path'].removeprefix('company-dash/') if source.get('project')=='company-dash' else source['path'])
    return f"[{display}](<{path}>)"

def find_line(source,anchor):
    return next(n for n,line in enumerate(source['text'].splitlines(),1) if anchor in line)

def fnt(size,bold=False):
    return ImageFont.truetype('C:/Windows/Fonts/'+('arialbd.ttf' if bold else 'arial.ttf'),size)

def wrapped(draw,value,font,width):
    out=[]
    for para in value.split('\n'):
        line=''
        for word in para.split():
            if draw.textlength(word,font=font)>width:
                if line:out.append(line);line=''
                fragment=''
                for character in word:
                    if fragment and draw.textlength(fragment+character,font=font)>width:
                        out.append(fragment);fragment=''
                    fragment+=character
                line=fragment
                continue
            test=(line+' '+word).strip()
            if draw.textlength(test,font=font)>width and line:out.append(line);line=word
            else:line=test
        out.append(line)
    return out

def box(d,rect,title,body,fill):
    x1,y1,x2,y2=rect;d.rounded_rectangle(rect,radius=12,fill=fill,outline='#8291a0',width=2)
    y=y1+15
    for value,size,bold in [(title,29,True),(body,23,False)]:
        for line in wrapped(d,value,fnt(size,bold),x2-x1-34):
            d.text((x1+17,y),line,font=fnt(size,bold),fill='#111111');y+=size+7
        y+=7
    if y>y2-8:raise ValueError('Diagram overflow '+title)

def arrow(d,x,y,end):
    d.line([(x,y),(end,y)],fill='#4f6477',width=4)
    d.polygon([(end,y),(end-12,y-8),(end-12,y+8)],fill='#4f6477')

def diagram(g):
    im=Image.new('RGB',(1540,800),'white');d=ImageDraw.Draw(im)
    d.text((30,20),f"{g['number']:02d} Follow the screen to its code",font=fnt(34,True),fill='black')
    box(d,(30,94,477,340),'1 Open the screen','\n'.join(g['routes'][:3]),'#e9f6ef')
    entry_label='2 Startup' if g['number'] in [2,30] else '2 Frontend'
    entry_body='Configures or prepares local services.' if g['number'] in [2,30] else 'Draws the screen and sends requests.'
    box(d,(547,94,994,340),entry_label,Path(g['frontend']).name+'\n'+entry_body,'#edf3fa')
    box(d,(1064,94,1510,340),'3 Backend',Path(g['backend']).name+'\nChecks requests and business rules.','#f1eff8')
    arrow(d,477,214,535);arrow(d,994,214,1052)
    box(d,(30,402,720,615),'Find the file in VS Code','Ctrl+P then paste its full repository path.\nUse Ctrl+F with the exercise search phrase.','#f6f7f9')
    model_body='Lists the projects in the backend solution.' if g['number']==2 else 'Stores or describes the records used by this feature.'
    box(d,(795,402,1510,615),'4 Data and shared rules',Path(g['model']).name+'\n'+model_body,'#e9f6ef')
    d.line([(1287,340),(1287,392)],fill='#4f6477',width=4)
    d.polygon([(1287,400),(1279,388),(1295,388)],fill='#4f6477')
    d.text((30,672),'Use the file map below for complete paths and source IDs.',font=fnt(28),fill='#111111')
    d.text((30,716),'Shared components can change more than one screen. Check the related guides.',font=fnt(26),fill='#111111')
    path=ASSETS/f"guide-{g['number']:02d}-map.png";im.save(path)
    return path.relative_to(OUT).as_posix()

def platform_diagram():
    im=Image.new('RGB',(1540,1120),'white');d=ImageDraw.Draw(im)
    d.text((30,20),'The three Nexora applications share the platform',font=fnt(34,True),fill='black')
    box(d,(30,90,477,320),'Staff CRM','3017\nContacts, Membership and operations\nAdministration and editors','#e9f6ef')
    box(d,(547,90,994,320),'Member Portal','3018\nPersonal and company journeys\nSeparate member access','#edf3fa')
    box(d,(1064,90,1510,320),'Public website and CMS','3019\nPublished visitor pages\nStaff website editor','#f1eff8')
    box(d,(150,405,1390,575),'One web source project','apps/web  uses Next.js, React, TypeScript and CSS.\nEach debug process has a separate build output and forwards API requests.','#f6f7f9')
    box(d,(150,640,750,840),'One shared API','5097  backend/src/Nexora.Api\nNine C# modules and shared services','#edf3fa')
    box(d,(830,640,1390,840),'Stored records','SQL Express NexoraDev locally\nNine contexts and separate schemas','#e9f6ef')
    for x in [253,770,1287]:d.line([(x,320),(x,390)],fill='#4f6477',width=4)
    d.line([(770,575),(770,607),(450,607),(450,632)],fill='#4f6477',width=4)
    arrow(d,750,733,820)
    box(d,(150,900,1390,1090),'Company Dash is a separate application',
        '8000  Node server and plain browser frontend in its own repository.\nInspects Nexora source and manages isolated proposals and SQLite task history.', '#f1eff8')
    path=ASSETS/'platform-map.png';im.save(path)
    return path.relative_to(OUT).as_posix()

GLOSSARY='''### The words used throughout the guides

| Word | Meaning |
| --- | --- |
| Frontend | Code that draws the screen and handles browser actions. |
| Backend | Server code that checks requests and stores or returns records. |
| Component | A reusable part of a screen, such as a form or toolbar. |
| Route | The address of a screen, such as /membership/members. |
| Endpoint | An API address and method, such as GET /api/v1/crm/records. |
| Request and response | The browser asks for something; the server returns a result. |
| JSON | Named data values sent between browser and API. |
| State | A value held by a screen while it runs, such as an open editor. |
| Hook | A React function such as useState or useEffect that manages screen behaviour. |
| Model | A description of the shape of stored or transferred data. |
| DbContext | A C# gateway to a module's database tables. |
| Migration | A reviewed set of database structure changes. |
| Tenant | One organisation's area and permitted records. |
| Permission | A named right checked by the server, such as content.publish. |
| Version | A marker which helps detect another user's newer edit. |
| Draft and published | Editable saved work and the version released for other people to see. |
| Git commit | A named local snapshot of selected project changes. |

### Read the smallest piece first

An import at the top of a .tsx file tells you which other components it uses. A function describes behaviour. return supplies the markup the screen draws. Text between tags is often visible wording; values inside braces come from code or data. onClick and onSubmit connect user actions to functions. await waits for a request result. A JSON key must match the server request model; changing a field label does not mean renaming that key.

CSS changes spacing, colour and movement. A className attaches styles to an element. A module.css file usually scopes styles to a component; a shared global stylesheet can affect many screens. A value such as var(--portal-brand) reads a configured colour variable. Database-backed portal and website settings can override defaults in source.

### Use VS Code one step at a time

1. Choose File then Open Folder and select D:\\Projects new\\Nexora. Confirm apps, backend and docs appear in Explorer.
2. Press Ctrl+P, paste a path from the guide, and press Enter. Paths in the guide are relative to this repository unless explicitly absolute.
3. Press Ctrl+F and search for the exercise phrase. Press Ctrl+G only when you want a numbered source line; line numbers move as code changes.
4. Read the whole surrounding tag or function. Record its original text before editing. Change one visible phrase first.
5. Press Ctrl+S to save. Read View then Problems. Refresh the local screen and inspect the changed wording.
6. Open Source Control to review your own diff. Revert only your own text when practising.

Explorer is the folder tree. The editor is where you read and type. Run and Debug is where you choose CRM, Portal or CMS. Terminal then New Terminal opens commands. Problems shows reported syntax and type issues. Saving a frontend text change normally refreshes during development; backend changes require a rebuilt or restarted API.

### Start the applications on this workstation

Choose CRM, Portal or CMS in Run and Debug, then press F5. Each profile prepares the shared API before starting the frontend. CRM uses http://localhost:3017, Portal uses http://localhost:3018/portal, and CMS uses http://localhost:3019. Staff editors remain at /portal-management and /website. The shared API uses http://localhost:5097 and the configured SQL Express NexoraDev database. The three frontends have distinct .next-local-debug output folders.

The All applications profile also launches Company Dash from its separate repository. That application is not Nexora source. To work only on Nexora, launch its three named profiles. Source and documentation in this manual remain within the Nexora repository.

The existing launch file and preparation script are the authority for this workstation. A new computer needs compatible Node, .NET, SQL and extensions plus adjusted runtime paths. Do not run a guessed command from an older README when its addresses differ. A directly started next dev can fall back to another API if NEXORA_API_URL was not provided.

### Check your work and restore a practice change

For a text change, reload the page, inspect a second screen that shares the component, check mobile layout if the text length changed, and review the diff. Do not submit real data to prove a wording edit. Ctrl+Z is suitable for a just-made edit in the open file; for a saved practice edit, restore the recorded original text and save. Source Control Discard is safe only if every change in that file belongs to you. Other people's unfinished work can share a file.

For a behaviour change, run the relevant type and module checks as well as the actual screen journey. Full integrated tests use isolated data and services; coordinate them with the team. Tests named in this manual are places to start, not claims that this documentation run executed them.

```powershell
npm --prefix apps/web exec tsc -- --noEmit
npm run lint:web
dotnet build backend/Nexora.slnx --configuration Release
```

Use those broad commands after meaningful code changes. Backend test filters and browser configuration need the appropriate developer review. Do not run migration or import commands simply to check a label edit.

### Understand common error responses

| Result | What to inspect |
| --- | --- |
| 400 invalid request | Field types, validation messages and CSRF token. |
| 401 sign-in required | The relevant staff or member session and expiry. |
| 403 permission denied | User group permissions and endpoint authorisation. |
| 404 unavailable record | Route, record id, tenant and explicit company grant. |
| 409 conflict | Reload the latest version before retrying a save. |
| 429 too many requests | Allow the login rate limit to reset; inspect the limit. |
| 503 service unavailable | API readiness, provider configuration, logs and SQL connection. |
| Quarantined file | Scanner state and clean-download rules; do not bypass them. |

'''

def excerpt(source,anchor=None):
    lines=source['text'].splitlines(); selected=[]
    if anchor:
        n=find_line(source,anchor); line=lines[n-1]
        if len(line)<=200:selected=[(n,line)]
        else:
            pos=line.index(anchor);start=max(0,pos-35);end=min(len(line),pos+len(anchor)+45)
            selected=[(n,line[start:end])]
        caption='Extract from source line '+str(n)+'. A fragment from a long line is shown without the rest of that line.'
    else:
        selected=[(n,line) for n,line in enumerate(lines[:35],1) if line.strip() and len(line)<=170][:6]
        caption='Selected opening lines with their original numbers. The complete file is in the HTML source library.'
    return caption+'\n\n```'+Path(source['path']).suffix[1:]+'\n'+'\n'.join(f'{n:04d}  {line}' for n,line in selected)+'\n```\n'

def chapter(g,bypath):
    n=g['number']; fid=bypath[g['frontend']];bid=bypath[g['backend']];mid=bypath[g['model']]
    path,anchor,after,instructions=g['exercise']; ex=bypath[path];line=find_line(ex,anchor)
    figure=diagram(g)
    rows=['| Role | File and source ID | What it does |','| --- | --- | --- |']
    entry_role='Startup' if n in [2,30] else 'Screen'
    data_role='Projects' if n==2 else 'Data'
    for label,s,purpose in [(entry_role,fid,'Local service setup' if n in [2,30] else 'Main UI or route for this topic'),('Server',bid,'Endpoints or server rules'),(data_role,mid,'Backend solution projects' if n==2 else 'Data shape or persistence structure')]:
        rows.append(f"| {label} | {s['id']} {Path(s['path']).name} | {purpose} |")
    is_dash=n>=31
    route_help=('Company Dash uses loopback port 8000 and hash navigation. Open its separate repository in VS Code before Ctrl+P. The source library uses company-dash as a project label, not a Nexora folder.' if is_dash else 'CRM routes use port 3017; member routes use 3018; public website routes can use 3019. Brackets name a route parameter, not text to paste into a browser. Choose a real record or site id from the application.')
    server_explanation=('The browser api() helper sends a method and local request token. app.mjs checks the route and calls the relevant service. Follow that call to understand SQL writes, process execution and filesystem changes.' if is_dash else 'On the server, look for MapGet to load records, MapPost to create or perform an action, MapPut or MapPatch to change data, and RequireAuthorization for permissions. Some shared methods register endpoints indirectly. Find SaveChangesAsync to see the database write. A request method alone does not tell you every side effect.')
    model_explanation=('store.mjs defines SQLite history tables. catalog.mjs and config.mjs define configuration; repository.mjs records fingerprints. Trace each key into its service before assuming it is a stored table.' if is_dash else 'Follow model fields to their request names and database mapping. An id points to a particular record. A tenant id limits the organisation. A version or concurrency stamp protects newer edits. Some model files describe contracts rather than tables; check the owning DbContext before assuming storage.')
    paragraphs=[f"<!-- CHAPTER {n:02d} -->",f"## {n:02d} {g['title']}",'',g['purpose'],'',
                '### Recognise the screen and its scope','',
                'Open '+', '.join('`'+r+'`' for r in g['routes'])+'. '+route_help,'',
                'This guide covers '+ '; '.join(g['scope'])+'.','',
                f'![File journey for {g["title"]}]({figure})','',
                'The diagram shows the file roles. The full paths below are what you paste into Ctrl+P.','',
                '### Find the files in VS Code','', '\n'.join(rows),'']
    for label,s in [(entry_role,fid),('Server',bid),(data_role,mid)]:paragraphs.extend([f"**{label} {s['id']}**  ",source_link(s),''])
    server_anchor=next((a for a in ['export function','export async function','enqueue(','MapGroup','MapGet','MapPost','AddNexora','public static','public sealed','public class'] if a in bid['text']),None)
    frontend_anchor={31:'<nav aria-label',32:'function knowledge()',33:'function work()',34:'function qaView()',35:'function proposalActions(',36:'function settings()'}.get(n)
    if is_dash:server_anchor={31:'export function createApp',32:'export async function readSource',33:'enqueue(',34:'async prepareQaRuntime',35:'export async function proposalReview',36:'export async function backupData'}[n]
    source_help=('Use the HTML source buttons for full files. Word shows labelled extracts; individual Markdown guides contain complete primary files.' if is_dash else 'The complete HTML manual opens these source IDs inside its Source library. The Word manual shows short labelled extracts so you can learn the flow before reading large files. Each individual Markdown guide includes its complete primary files after the walkthrough.')
    frontend_help=('index.html supplies markup and app.js renders browser views. Follow api() to the server route handler.' if is_dash else 'The import lines lead to shared components and helpers. Start at the exported component or route function; follow the request helper to the server rather than assuming the whole feature lives in this file.')
    paragraphs.extend([source_help,'',
                       '### Follow the request and the records',''])
    paragraphs.extend(f'{i}. {sentence}' for i,sentence in enumerate(g['journey'],1))
    paragraphs.extend(['','### Read the code with an explanation','',g['explanation'],'',
                       f"Source reference {fid['id']} {fid['path']}",'',excerpt(fid,frontend_anchor),'',
                       frontend_help,'',
                       f"Source reference {bid['id']} {bid['path']}",'',excerpt(bid,server_anchor),'',
                       server_explanation,'',
                       f"Source reference {mid['id']} {mid['path']}",'',excerpt(mid),'',
                       model_explanation,'',
                       '### Make or trace your first change','',
                       f"1. Press Ctrl+P and open {source_link(ex)}.",
                       f"2. Press Ctrl+F and search for `{anchor}`. In this snapshot it appears at line {line}.",
                       '3. '+instructions])
    if after:
        paragraphs.extend([f'4. Replace only `{anchor}` with `{after}` in that intended location. Keep quotes, tags, JSON keys and routes intact.',
                           '5. Save with Ctrl+S. Inspect Problems and the local screen. Review the diff and restore the original text when you finish practising.'])
    else:paragraphs.append('4. Record the file, request path and server handler you found. This exercise is read-only; no stored records need to change.')
    paragraphs.extend(['',excerpt(ex,anchor),'',
                       'These exercises have not been applied to the application. Their search phrases are verified against the captured source. Line numbers can move; the phrase and file path are your main navigation aids.','',
                       '### Check a change and diagnose problems','',
                       'Confirm the original screen still loads, the changed words appear in the intended place, and another screen using the same component still behaves correctly. If an action fails, inspect the browser Network response, the relevant helper and server validation message. Use the common error table at the beginning of the manual. Reload on a version conflict instead of removing the version check.','',
                       'A syntax error after a text edit usually means a quote, brace or closing tag changed. Compare your diff with the original. An unchanged browser result can mean the wrong port, a saved published configuration overriding a default, or a running API that has not reloaded.','',
                       '### Understand the current limits','',g['limits'],'',
                       '### Continue learning','',
                       'Related guides '+', '.join(f"[{i:02d} {GUIDES[i-1]['title']}](#guide_{i:02d})" for i in g['related'])+'.' if g['related'] else 'Use the complete source library and coverage index for the related files.',''])
    return '\n'.join(paragraphs)

def route_name(path):
    rest=path.removeprefix('apps/web/src/app/')
    parts=[p for p in rest.split('/')[:-1] if not (p.startswith('(') and p.endswith(')'))]
    return '/'+('/'.join(parts))

def inventory(sources,routes,modules,contexts):
    result=['<!-- CHAPTER inventory -->','## Platform coverage and source index','',
            'The following inventory is generated from this source snapshot. Every captured text source file has a source ID and at least one teaching chapter. Shared files may support several chapters. Framework package internals, binary media, generated build output and local secret files are outside the source library.','',
            '### Screen routes and shared layouts','',
            'page.tsx is a screen entry. route.ts handles an HTTP route such as media or sitemap. layout.tsx surrounds child pages. The dynamic [workspace] fallback is a retained scheduled-module placeholder; dedicated implemented routes take precedence.','',
            '| Route or layout scope | Entry source ID | Guide |','| --- | --- | --- |']
    for s in routes:
        result.append(f"| {route_name(s['path'])} {Path(s['path']).name} | {s['id']} | {', '.join(str(i).zfill(2) for i in s['chapters'])} |")
    result.extend(['','### Backend modules','', '| Module | Source directory | Guide |','| --- | --- | --- |'])
    for mod in modules:
        directory='backend/src/Modules/'+mod
        result.append(f"| {mod.removeprefix('Nexora.Modules.')} | {directory} | {', '.join(str(i).zfill(2) for i in owners(directory+'/'))} |")
    result.extend(['','### Database contexts','',
                   f"The current source contains {len(contexts)} concrete module contexts. Identity uses its own Identity mapping and migration history. Other modules configure their own schemas and histories. OperationalChecks checks each context rather than assuming one healthy connection proves all tables are ready.",'',
                   '| Context | Owning module | Source ID |','| --- | --- | --- |'])
    bypath={s['path']:s for s in sources}
    for name,path in contexts:result.append(f"| {name} | {path.split('Nexora.Modules.')[1].split('/')[0]} | {bypath[path]['id']} |")
    result.extend(['','### Company Dash views and services','',
                   'Company Dash is served by its own Node server at 127.0.0.1:8000. These hash views are defined in public/index.html and public/app.js; app.mjs maps API requests. Its Store uses SQLite, separately from the Nexora SQL contexts above.','',
                   '| View or service | Implementation | Guide |','| --- | --- | --- |',
                   '| Overview and startup | public/index.html and server/app.mjs | 31 |',
                   '| Repository and docs | public/app.js and server/repository.mjs | 32 |',
                   '| Work board and AI team | server/runner.mjs store.mjs and catalog.mjs | 33 |',
                   '| QA and verification | server/runner.mjs and catalog.mjs | 34 |',
                   '| Proposal review and apply | server/proposals.mjs | 35 |',
                   '| Activity settings and backups | server/maintenance.mjs usage.mjs and store.mjs | 36 |',
                   '', '### Find all supporting code','',
                   f"The offline HTML Source library contains {len(sources)} text files and {sum(s['lines'] for s in sources):,} lines. It includes production frontend and backend source, EF migrations, related test source and selected build/debug configuration. Search by source ID, path or a phrase inside code; select a guide to narrow the list. Each selected file displays its full captured code, numbered lines and SHA256 fingerprint. Configured local passwords are replaced by an explicit omitted marker, and those files are labelled redacted.",'',
                   'The source library is part of the HTML file itself. It needs no server, network or dependency download. Its code is a dated reference; edit the actual repository file in VS Code. Copy raw source excludes the display line numbers. Open in VS Code is a user-clicked editor link and may prompt your browser.',''])
    return '\n'.join(result)

def write_readme(manifest):
    rows=['# Nexora beginner documentation','',
          f"Start with **{MASTER}.html**. This one offline file contains all {len(GUIDES)} teaching guides, embedded diagrams, VS Code maps and the complete captured code library. Guides 01 to 30 cover Nexora CRM, Portal, CMS, public website and backend. Guides 31 to 36 cover the separate Company Dash application.",'',
          f'The **{MASTER}.docx** is the editable reading edition with short labelled code extracts. Full files are embedded in the HTML library. Individual Markdown guides append their complete primary implementation files; the originally approved login guide remains a dated reference.','',
          f'- [Complete offline guide with full code]({MASTER.replace(" ","%20")}.html)',
          f'- [Combined Word manual]({MASTER.replace(" ","%20")}.docx)',
          f'- [Editable teaching source]({MASTER.replace(" ","%20")}.md)',
          '- [Maintenance and automatic refresh](DOCUMENTATION-MAINTENANCE.md)',
          '- [Source manifest](source-manifest.json)','- [Route coverage](coverage.json)','',
          '## Individual guides','', '| Guide | Word | Teaching and primary code |','| --- | --- | --- |']
    for g in GUIDES:
        stem=('Nexora login page' if g['number']==1 else f"chapters/{g['number']:02d} {g['title']}").replace(' ','%20')
        rows.append(f"| {g['number']:02d} {g['title']} | [Word]({stem}.docx) | [Markdown]({stem}.md) |")
    rows.extend(['','## Snapshot and verification','',
                 f"Captured {manifest['captured_at_utc']}: {manifest['file_count']} text files and {manifest['source_lines']:,} lines across the two application repositories. The Nexora inventory includes {manifest['route_entries']} page/layout/HTTP entries, {len(manifest['modules'])} modules and {len(manifest['contexts'])} concrete database contexts.",'',
                 'The source snapshot includes allowed working-tree changes. Configured local passwords are omitted and marked; private local configuration, runtime databases, dependency/build folders and binary media are excluded. Company Dash is read for documentation only and stays a separate project. Capture metadata and SHA256 fingerprints identify the copied version; this documentation introduces no runtime dependency on its checkout.','',
                 'Browser, source-copy and Word layout checks are documentation evidence. They do not prove that product regression suites, live providers or production acceptance ran again. See each guide for current limits.','',
                 '## Maintenance','',
                 'Use scripts/docs/Sync-PlatformGuides.py --check to detect changed, added or removed source and editorial changes. --refresh archives the previous complete manual before regenerating it. Review changed explanations and run the documented browser and Word checks. The hourly chat automation performs this workflow when local files are available; ongoing Nexora work must also follow the documentation rule in AGENTS.md.','',
                 'The maintained filenames remain the same. Dated complete-manual copies are in history. See DOCUMENTATION-MAINTENANCE.md for commands, project paths, review steps and limitations.'])
    (OUT/'README.md').write_text('\n'.join(rows)+'\n',encoding='utf-8')

MAINTENANCE='''<!-- CHAPTER maintenance -->
## How to maintain the platform and these guides

### Assemble the platform from its dependencies

This is a teaching order, not a claim about the historical day-by-day build. Start with repository structure and dependencies. Then understand the shared API and tenant identity, the staff shell, CRM records and module data, member access, content publishing, automation configuration and the workflow processor. The test source and release runbook explain how to verify boundaries before deployment.

A screen needs its imports, packages, request helpers, API endpoints, models, database mappings and configured environment. Copying a page.tsx file by itself does not recreate the platform. Shared BuildingBlocks are explicit code in this repository; Nexora has no source or runtime dependency on the sibling SalesApi project.

### Make a complete behaviour change

1. Explain the intended before and after behaviour using one screen and one example record.
2. Trace the current route, component, helper, endpoint, model and database mapping using the guide and source library.
3. Decide whether this is a stored configuration change, display wording change, data model change or business rule change.
4. For a new stored field, align frontend types, request models, validation, persistence and any required EF migration. Inspect the owning module and context.
5. For a new route or action, retain session, CSRF, permission and tenant checks. Understand related history, workflow triggers and notifications.
6. Use approved test data and relevant source tests. Inspect browser behaviour as well as build results. Reconcile data-changing operations separately.
7. Review the actual diff, update the corresponding guide and capture fresh source. Keep unverified work labelled as unverified.

### Save only your work in Git

Git records selected changes in a local commit. A push sends commits to a remote. A deployment changes a running environment. These are separate actions. Check git status before and after work. Review the exact files you intend to stage. Do not use git add . in a shared checkout that has unrelated unfinished work.

```powershell
git status --short
git diff -- path/to/your/file
git add -- path/to/your/file
git diff --cached --stat
git commit -m "docs: update the relevant Nexora beginner guide"
```

The example path is a placeholder for your own file. If a file contains another person's changes, coordinate before staging it. Restoring or discarding a whole file can remove work you did not create. The documentation builder does not commit or push automatically.

### Rebuild the manual

The editorial explanations are in scripts/docs/platform_guide_catalog.py. The Markdown to Word renderer is scripts/docs/guide_docx.py. The master generator is scripts/docs/Build-PlatformGuides.py. Keep these with the documents so another maintainer can reproduce the output.

```powershell
& 'C:\\Users\\ramish\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\python\\python.exe' scripts/docs/Build-PlatformGuides.py
```

The runtime path is specific to this workstation. The builder captures code, verifies every primary file and exercise search phrase, generates diagrams, writes the individual guides, and combines the complete Word, Markdown and offline HTML manuals. It excludes build output and checks that configured local password values were not copied. Review new source changes, scope and rendered pages before delivering a rebuilt Word file.

### Keep the maintained copy current

The complete manual keeps the same filename so saved links remain useful. scripts/docs/Sync-PlatformGuides.py --check compares the captured source fingerprints with both live repositories and detects added or removed files. It also checks the editorial catalogue and document tooling. When changes exist, inspect the changed behaviour and update the explanation, map or exercise before running --refresh.

Refresh saves the previous complete HTML, Word and Markdown manuals, their manifests and diagrams in docs/beginner-guides/history. It then rebuilds the maintained copy. If authoring fails, the last complete collection is restored from an internal backup. Browser and Word page checks remain required; a copied source change alone does not prove that its explanation is correct.

The hourly documentation check returns to this chat and runs only while the computer and Codex app can access local files. It stays quiet when nothing changes. This is a scheduled refresh, not an instantaneous file watcher or database audit. The manual records source and selected configuration; saved CRM business records are not copied into it. See DOCUMENTATION-MAINTENANCE.md for the maintainer commands and review state.

### Preserve the evidence boundaries

Source inspection proves where implemented code is located. A build proves compilation for that build. A browser journey proves the exercised flow in its tested environment. A stored queue item proves queued work. Provider acknowledgements and reconciliation are needed for external delivery. Production approval also needs identity, hosting, backup, restore and operational acceptance evidence.

This documentation task checks guide paths, search phrases, coverage, copied source and document rendering. It does not claim that all module tests or every live journey were executed again. Tests in the source library are provided for learning and future verification.

### Find the deeper technical records

Existing technical material remains useful after the beginner map makes the code familiar. Start with docs/LOCAL-TESTING.md, docs/blueprint/NEXORA_PRODUCT_BLUEPRINT.md, docs/stages, docs/architecture and docs/operations/RELEASE-RUNBOOK.md. Migration mapping inventories are in docs/strategy and delivery evidence is in docs/implementation. Those records contain dated results; compare them with current code before treating an older result as current verification.
'''

def inline_html(value):
    # Escape first; allow only links produced by the guide's controlled renderer.
    tokens=[]
    def save(m):
        target=m[2] or m[3];label=m[1]
        tokens.append('<a href="'+html.escape(target,quote=True)+'">'+html.escape(label)+'</a>')
        return '\x00'+str(len(tokens)-1)+'\x00'
    value=re.sub(r'\[([^\]]+)\]\((?:<([^>]+)>|([^)]*))\)',save,value)
    value=html.escape(value)
    value=re.sub(r'\*\*(.+?)\*\*',r'<strong>\1</strong>',value)
    value=re.sub(r'`([^`]+)`',r'<code>\1</code>',value)
    for i,t in enumerate(tokens):value=value.replace('\x00'+str(i)+'\x00',t)
    return value

def md_html(markdown):
    lines=markdown.splitlines();output=[];i=0;in_list=False;list_tag='';heading_ids=set()
    def close_list():
        nonlocal in_list
        if in_list:output.append('</'+list_tag+'>');in_list=False
    while i<len(lines):
        line=lines[i]
        if line.startswith('<!--'):i+=1;continue
        if line.startswith('```'):
            close_list();code=[];i+=1
            while i<len(lines) and not lines[i].startswith('```'):code.append(lines[i]);i+=1
            output.append('<pre class="extract"><code>'+html.escape('\n'.join(code))+'</code></pre>');i+=1;continue
        if not line.strip():close_list();i+=1;continue
        if line.startswith('|'):
            close_list();rows=[]
            while i<len(lines) and lines[i].startswith('|'):rows.append(lines[i].strip().strip('|').split('|'));i+=1
            output.append('<div class="table-wrap"><table><thead><tr>'+''.join('<th>'+inline_html(x.strip())+'</th>' for x in rows[0])+'</tr></thead><tbody>')
            for row in rows[2:]:output.append('<tr>'+''.join('<td>'+inline_html(x.strip())+'</td>' for x in row)+'</tr>')
            output.append('</tbody></table></div>');continue
        m=re.match(r'^(#{1,5}) (.*)',line)
        if m:
            close_list();depth=len(m[1]);text=m[2];id_match=re.match(r'(\d{2}) (?:Nexora|Company Dash)',text)
            ident='guide_'+id_match[1] if id_match else re.sub(r'[^a-z0-9]+','-',text.lower()).strip('-')
            base=ident;duplicate=2
            while ident in heading_ids:ident=base+'-'+str(duplicate);duplicate+=1
            heading_ids.add(ident)
            output.append(f'<h{depth} id="{ident}">'+inline_html(text)+f'</h{depth}>')
        elif line.startswith('!['):
            close_list();m=re.match(r'!\[([^]]+)\]\(([^)]+)\)',line)
            img=OUT/m[2];data=base64.b64encode(img.read_bytes()).decode()
            output.append('<figure><img src="data:image/png;base64,'+data+'" alt="'+html.escape(m[1],quote=True)+'"></figure>')
        elif re.match(r'^\d+\. ',line) or line.startswith('- '):
            ordered=not line.startswith('- ');tag='ol' if ordered else 'ul'
            if in_list and tag!=list_tag:close_list()
            if not in_list:output.append('<'+tag+'>');in_list=True;list_tag=tag
            value=re.sub(r'^(?:\d+\. |- )','',line);output.append('<li>'+inline_html(value)+'</li>')
        else:close_list();output.append('<p>'+inline_html(line.rstrip())+'</p>')
        i+=1
    close_list();return '\n'.join(output)

CSS='''
:root{color-scheme:light;--ink:#162637;--muted:#526475;--line:#d9e1e8;--paper:#fff;--accent:#285b49}
*{box-sizing:border-box}html{scroll-behavior:smooth;scroll-padding-top:84px}body{margin:0;background:#f3f6f8;color:var(--ink);font:17px/1.65 Arial,sans-serif}
header.top{position:sticky;top:0;z-index:5;background:#fff;border-bottom:1px solid var(--line);padding:12px 25px;display:flex;align-items:center;gap:22px;flex-wrap:wrap}
.brand{font-weight:700;font-size:20px;color:#111}nav.tabs{display:flex;gap:6px}button,.button{font:inherit;cursor:pointer;border:1px solid #b8c7d1;border-radius:6px;padding:7px 13px;background:#fff;color:#162637;text-decoration:none}button[aria-pressed=true]{background:#e5f1ea;border-color:#58846e;font-weight:bold}button:focus-visible,a:focus-visible,input:focus-visible,select:focus-visible{outline:3px solid #157ed0;outline-offset:3px}
.layout{display:grid;grid-template-columns:305px minmax(0,1fr);max-width:1500px;margin:auto}.sidebar{position:sticky;top:74px;height:calc(100vh - 74px);overflow:auto;padding:24px 20px;border-right:1px solid var(--line)}.sidebar a{display:block;padding:5px 4px;font-size:14px;line-height:1.4;color:#263d55;text-decoration:none}.sidebar a:hover{background:#e5edf2}.sidebar input{width:100%;margin-bottom:14px;padding:9px;border:1px solid #a6b7c4;border-radius:5px;font:inherit}
main{min-width:0;padding:35px 50px;background:#fff}h1{color:#000;font-size:36px;line-height:1.2;margin:0 0 22px}h2{color:#000;font-size:27px;line-height:1.3;margin:55px 0 20px;border-top:1px solid var(--line);padding-top:28px}h3{color:#000;font-size:21px;line-height:1.4;margin:30px 0 12px}p{margin:12px 0 18px;overflow-wrap:anywhere}li{margin:9px 0}a{color:#24557b;overflow-wrap:anywhere}code{font-family:Consolas,monospace;font-size:.9em;overflow-wrap:anywhere}.extract{white-space:pre-wrap;overflow-wrap:anywhere;background:#f7f9fb;padding:15px;border-left:3px solid #7b9baa;font:14px/1.55 Consolas,monospace}
figure{margin:22px 0}figure img{width:100%;height:auto;display:block}.table-wrap{overflow:auto;margin:20px 0}table{border-collapse:collapse;width:100%;font-size:15px}td,th{border:1px solid var(--line);padding:9px 12px;text-align:left;vertical-align:middle;overflow-wrap:anywhere}th{background:#e8eef3;color:#111}.small{font-size:14px;color:var(--muted)}[hidden]{display:none!important}
.source-controls{display:flex;gap:12px;flex-wrap:wrap;align-items:end;margin:20px 0}.source-controls label{flex:1;min-width:220px}.source-controls input,.source-controls select{width:100%;font:inherit;padding:10px;border:1px solid #a6b7c4;border-radius:5px}.source-list{max-height:330px;overflow:auto;border:1px solid var(--line);border-radius:5px}.source-list button{width:100%;text-align:left;border:0;border-radius:0;border-bottom:1px solid var(--line);font-size:14px;overflow-wrap:anywhere}.source-list button:hover{background:#edf4f7}.source-meta{overflow-wrap:anywhere}.source-actions{display:flex;gap:10px;flex-wrap:wrap;margin:16px 0}.source-code{background:#f7f9fb;border:1px solid var(--line);padding:16px;white-space:pre-wrap;overflow-wrap:anywhere;font:14px/1.55 Consolas,monospace;max-height:75vh;overflow:auto}.status{min-height:26px;font-size:14px;color:#285b49}.chapter-code-link{display:inline-block;margin:5px 0 15px}
@media(max-width:1000px){.layout{grid-template-columns:230px minmax(0,1fr)}main{padding:30px}.sidebar{padding:18px 12px}}@media(max-width:700px){body{font-size:16px}.layout{display:block}.sidebar{position:static;height:auto;max-height:270px;border-right:0;border-bottom:1px solid var(--line)}main{padding:24px 18px}h1{font-size:29px}h2{font-size:24px}header.top{padding:10px 15px;gap:8px}.brand{font-size:18px}}
@media print{header.top,.sidebar,.source-controls,.source-list,.source-actions{display:none!important}.layout{display:block}main{padding:0}body{background:white;font:11pt/1.4 Arial}h2{break-before:page}figure{break-inside:avoid}.source-code{max-height:none;overflow:visible}table{font-size:9pt}a{color:#111}pre{font-size:8pt}}
'''

JS=r'''
const files=JSON.parse(document.getElementById('source-data').textContent);
const el=id=>document.getElementById(id);let selected=null;
function showMode(mode){el('learn-panel').hidden=mode!=='learn';el('source-panel').hidden=mode!=='source';document.querySelectorAll('[data-mode]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.mode===mode)));}
document.querySelectorAll('[data-mode]').forEach(b=>b.addEventListener('click',()=>showMode(b.dataset.mode)));
function results(){const q=el('source-search').value.toLowerCase().trim(),guide=Number(el('source-guide').value);const found=files.filter(f=>(!guide||f.chapters.includes(guide))&&(!q||f.id.toLowerCase().includes(q)||f.path.toLowerCase().includes(q)||f.text.toLowerCase().includes(q)));el('source-results').replaceChildren();found.forEach(f=>{const b=document.createElement('button');b.textContent=f.id+'  '+f.path;b.type='button';b.addEventListener('click',()=>openSource(f.id));el('source-results').append(b)});el('source-count').textContent=found.length+' matching files';}
function openSource(id,line){const f=files.find(f=>f.id===id);if(!f)return;selected=f;showMode('source');el('source-title').textContent=f.id+'  '+f.path;el('source-meta').textContent=f.lines+' source lines · '+f.kind+' · '+(f.redacted?'Configured local password omitted · ':'')+'SHA256 '+f.sha256;el('source-code').textContent=f.text.split(/\r?\n/).map((t,i)=>String(i+1).padStart(5,'0')+'  '+t).join('\n');el('editor-link').href='vscode://file/'+encodeURI(f.absolute_path||'D:/Projects new/Nexora/'+f.path)+(line?':'+line:'');el('source-actions').hidden=false;el('source-status').textContent='Full captured file shown. Edit the live repository file in VS Code.';if(line)el('source-code').scrollTop=Math.max(0,(line-3)*22);else el('source-code').scrollTop=0;window.scrollTo({top:0,behavior:'smooth'});}
el('source-search').addEventListener('input',results);el('source-guide').addEventListener('change',results);
el('chapter-search').addEventListener('input',e=>{const q=e.target.value.toLowerCase();document.querySelectorAll('.chapter-nav').forEach(a=>a.hidden=!a.textContent.toLowerCase().includes(q));});
document.querySelectorAll('[data-source]').forEach(b=>b.addEventListener('click',()=>openSource(b.dataset.source,Number(b.dataset.line)||undefined)));
document.querySelectorAll('.chapter-nav').forEach(a=>a.addEventListener('click',()=>showMode('learn')));
document.querySelectorAll('#learn-panel a[href]').forEach(a=>{const href=a.getAttribute('href'),m=href.match(/^(.*?)(?::(\d+))?$/);const f=files.find(f=>(f.absolute_path||'D:/Projects new/Nexora/'+f.path)===m[1]);if(f){a.href='#source';a.addEventListener('click',e=>{e.preventDefault();openSource(f.id,Number(m[2])||undefined);});}});
el('copy-source').addEventListener('click',async()=>{if(!selected)return;try{await navigator.clipboard.writeText(selected.text);el('source-status').textContent='Raw source copied without display line numbers.';}catch{el('source-status').textContent='Clipboard is unavailable on this file URL. Use Download source instead.';}});
el('download-source').addEventListener('click',()=>{if(!selected)return;const a=document.createElement('a'),url=URL.createObjectURL(new Blob([selected.text],{type:'text/plain;charset=utf-8'}));a.href=url;a.download=selected.path.split('/').pop();a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);el('source-status').textContent='Downloaded the captured source. Review before using it in another checkout.';});
results();
'''

def build_html(master,chapters,sources):
    nav=''.join(f'<a class="chapter-nav" href="#guide_{g["number"]:02d}">{g["number"]:02d} {html.escape(g["title"])}</a>' for g in GUIDES)
    options='<option value="0">All guides</option>'+''.join(f'<option value="{g["number"]}">{g["number"]:02d} {html.escape(g["title"])}</option>' for g in GUIDES)
    content=md_html(master)
    # Put a direct full-source button immediately after each chapter heading.
    for g in GUIDES:
        marker=f'<h2 id="guide_{g["number"]:02d}">';start=content.index(marker);end=content.index('</h2>',start)+5
        ids=[next(s['id'] for s in sources if s['path']==g[key]) for key in ['frontend','backend','model']]
        buttons='<div class="source-actions">'+''.join(f'<button type="button" data-source="{fid}">Read {role} code {fid}</button>' for fid,role in zip(ids,['frontend','backend','data']))+'</div>'
        content=content[:end]+buttons+content[end:]
    payload=json.dumps(sources,ensure_ascii=False).replace('<','\\u003c').replace('>','\\u003e').replace('&','\\u0026')
    return '<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+MASTER+'</title><style>'+CSS+'</style></head><body><header class="top"><span class="brand">Nexora complete beginner guide</span><nav class="tabs" aria-label="Manual views"><button type="button" data-mode="learn" aria-pressed="true">Learning guides</button><button type="button" data-mode="source" aria-pressed="false">Full source library</button></nav></header><div class="layout"><aside class="sidebar"><label for="chapter-search">Find a guide</label><input id="chapter-search" type="search" placeholder="Portal, finance, login…"><a href="#nexora-complete-beginner-guide">Start here</a>'+nav+'</aside><main><article id="learn-panel">'+content+'</article><section id="source-panel" hidden><h1>Full source library</h1><p>Read the full frontend and backend code captured with this manual. Source is embedded in this one file. Display line numbers are navigation aids; edit the live files in VS Code.</p><div class="source-controls"><label for="source-search">Find a file or code phrase<input id="source-search" type="search" placeholder="S0001, member-portal.tsx, MapPost…"></label><label for="source-guide">Narrow to a guide<select id="source-guide">'+options+'</select></label></div><p id="source-count" role="status" class="small"></p><div id="source-results" class="source-list" aria-label="Matching source files"></div><h2 id="source-title">Choose a source file</h2><p id="source-meta" class="small source-meta"></p><div id="source-actions" class="source-actions" hidden><a id="editor-link" class="button">Open in VS Code</a><button id="copy-source" type="button">Copy raw source</button><button id="download-source" type="button">Download source</button></div><p id="source-status" class="status" role="status"></p><pre id="source-code" class="source-code" aria-label="Numbered source code"></pre></section></main></div><script id="source-data" type="application/json">'+payload+'</script><script>'+JS+'</script></body></html>'

def run():
    global DATE
    parser=argparse.ArgumentParser();parser.add_argument('--audit',action='store_true');parser.add_argument('--html-only',action='store_true');parser.add_argument('--preserve-source',action='store_true');args=parser.parse_args()
    if args.html_only:
        existing=(OUT/(MASTER+'.html')).read_text(encoding='utf-8')
        payload=re.search(r'<script id="source-data" type="application/json">(.*?)</script>',existing,re.S)[1]
        sources=json.loads(payload)
        master=(OUT/(MASTER+'.md')).read_text(encoding='utf-8')
        (OUT/(MASTER+'.html')).write_text(build_html(master,[],sources),encoding='utf-8')
        print(json.dumps(dict(html_refreshed=True,source_snapshot_preserved=True,files=len(sources))));return
    if args.preserve_source:
        existing=(OUT/(MASTER+'.html')).read_text(encoding='utf-8')
        payload=re.search(r'<script id="source-data" type="application/json">(.*?)</script>',existing,re.S)[1]
        sources=json.loads(payload);secrets=[]
        prior=json.loads((OUT/'source-manifest.json').read_text(encoding='utf-8'))
        DATE=prior['date'];captured_at=prior.get('captured_at_utc')
    else:sources,secrets=capture()
    if not args.preserve_source:captured_at=datetime.now(timezone.utc).isoformat(timespec='seconds')
    capture_label=DATE+(' at '+datetime.fromisoformat(captured_at).strftime('%H:%M')+' UTC' if captured_at else '')
    bypath,routes,modules,contexts=audit(sources)
    if args.audit:print(json.dumps(dict(files=len(sources),lines=sum(s['lines'] for s in sources),routes=len(routes),modules=modules,contexts=contexts),indent=2));return
    for d in [OUT,CHAPTERS,ASSETS,QA]:d.mkdir(parents=True,exist_ok=True)
    head=prior['git_head_at_capture'] if args.preserve_source else subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
    status=subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True)
    mapping=platform_diagram()
    front=[f'# {MASTER}','',f'Source snapshot {capture_label}  ',
           'A learning manual for someone starting without coding experience','',
           f'This manual explains Nexora and its separate Company Dash maintenance application through {len(GUIDES)} beginner guides. Each guide identifies its screen, maps the frontend and backend files, follows the request into stored records, explains the code and gives a small practice task. The same structure as the approved Nexora login page guide is used throughout.','',
           'Guides 01 to 30 cover CRM, Portal, CMS, public website and the shared Nexora backend. Guides 31 to 36 cover Company Dash, including its complete frontend and backend source. The Company Dash source is a versioned documentation snapshot from its own repository; Nexora has no runtime dependency on that checkout.','',
           'The Word edition is the readable teaching manual with short labelled code extracts. The self-contained HTML edition combines that same teaching text with the full captured source library. Open the HTML file in a browser, choose Full source library and read or search every included file without needing a server. Individual Word and Markdown guides are also saved alongside this manual.','',
           'The snapshot reflects the working checkout, including implemented changes that may not yet be committed as application source. It is dated evidence, not a production release certificate. The documentation commit records the manuals; it does not silently commit other application edits. The originally approved login document is retained as its own dated reference.','',
           f'![Nexora applications and shared platform]({mapping})','',
           'Read the first login guide, then the platform setup and navigation guides. Continue with the screen you want to change. The full-code library lets you look beyond the short extracts when you are ready.','',
           '## Guide contents','']
    front.extend(f"{g['number']}. [{g['number']:02d} {g['title']}](#guide_{g['number']:02d})" for g in GUIDES)
    front.extend(['','## Start learning and run the platform','',GLOSSARY,''])
    sections=[]
    for g in GUIDES:
        teaching=chapter(g,bypath)
        if g['number']==1:
            approved=(OUT/'Nexora login page.md').read_text(encoding='utf-8').split('## 16 Source code appendix')[0]
            approved=approved.split('## 1 How to use this document',1)[1]
            # Avoid duplicate printed contents; preserve the approved walkthrough.
            approved=approved.split('## 2 The words you need first',1)[1]
            approved='## 2 The words you need first'+approved
            approved=re.sub(r'^## (.+)$',r'### \1',approved,flags=re.M)
            approved=re.sub(r'^### (.+)$',r'#### \1',approved,flags=re.M)
            # Chapter 1 has the new verified map and full approved learning content.
            teaching=teaching.split('### Read the code with an explanation')[0]+'### The approved login walkthrough\n\n'+approved
            teaching+='\n\n### Complete login source\n\nUse the frontend and backend buttons in the HTML manual for the current full files. The separate approved login document retains its original full source appendix.\n'
        sections.append(teaching)
        if g['number']>1:
            individual='# '+g['title']+'\n\nDocument '+str(g['number'])+' of the Nexora learning guides  \nSource snapshot '+capture_label+'\n\n'+teaching.replace(f"<!-- CHAPTER {g['number']:02d} -->\n## {g['number']:02d} {g['title']}\n",'')
            # Per-topic editable Markdown also contains complete primary files.
            appendix=['','## Complete primary source files','',
                      'These are the complete captured files, with numbered source lines. Other supporting files are included in the complete HTML manual. Configured local passwords, if any, are explicitly omitted. Do not paste the display line numbers into code.','']
            for path in dict.fromkeys(g[key] for key in ['frontend','backend','model']):
                s=bypath[path];appendix.extend([f"### {s['id']} {Path(path).name}",'',source_link(s),'',
                                              f"SHA256 `{s['sha256']}`; {s['lines']} source lines; "+('local password omitted' if s['redacted'] else 'complete captured text')+'.','',
                                              '```'+Path(path).suffix[1:]])
                appendix.extend(f'{i:04d}  {line}' for i,line in enumerate(s['text'].splitlines(),1));appendix.extend(['```',''])
            stem=f"{g['number']:02d} {g['title']}"
            (CHAPTERS/(stem+'.md')).write_text(individual.replace('](assets/','](../assets/')+'\n'.join(appendix),encoding='utf-8')
            build_docx(individual,CHAPTERS/(stem+'.docx'),OUT,g['title'])
    master='\n'.join(front)+'\n'+'\n'.join(sections)+'\n'+inventory(sources,routes,modules,contexts)+'\n'+MAINTENANCE
    html_output=build_html(master,sections,sources)
    for secret in secrets:
        if secret in html_output or secret in master:raise ValueError('Local configured password copied')
    (OUT/(MASTER+'.md')).write_text(master,encoding='utf-8')
    (OUT/(MASTER+'.html')).write_text(html_output,encoding='utf-8')
    build_docx(master,OUT/(MASTER+'.docx'),OUT,MASTER)
    manifest=dict(date=DATE,captured_at_utc=captured_at,git_head_at_capture=head,working_tree_snapshot=True,
                  guide_count=len(GUIDES),route_entries=len(routes),modules=modules,contexts=contexts,
                  file_count=len(sources),source_lines=sum(s['lines'] for s in sources),
                  projects=[dict(id='nexora',root=ROOT.as_posix(),source_files=sum(s.get('project','nexora')=='nexora' for s in sources)),dict(id='company-dash',root=COMPANY_DASH_ROOT.as_posix(),source_files=sum(s.get('project')=='company-dash' for s in sources))],
                  editorial_fingerprints=editorial_fingerprints(),
                  files=[{k:v for k,v in s.items() if k!='text'} for s in sources])
    (OUT/'source-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    coverage=dict(routes=[dict(path=s['path'],route=route_name(s['path']),source_id=s['id'],guides=s['chapters']) for s in routes],
                  uncovered_routes=[],uncovered_source_files=[],all_exercise_anchors_found=True,
                  all_primary_paths_found=True,configured_password_values_included=False,
                  live_business_actions_performed=False,application_regression_run=False)
    (OUT/'coverage.json').write_text(json.dumps(coverage,indent=2),encoding='utf-8')
    write_readme(manifest)
    (QA/'captured-status.txt').write_text(status,encoding='utf-8')
    (QA/'source-data.json').write_text(json.dumps(sources,ensure_ascii=False),encoding='utf-8')
    summary={k:v for k,v in manifest.items() if k not in ['files','contexts','modules']}
    (QA/'build-evidence.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    print(json.dumps(summary,indent=2))

if __name__=='__main__':run()
