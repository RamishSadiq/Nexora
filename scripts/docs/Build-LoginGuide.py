"""Build the beginner login guide from Markdown and the current Nexora source.

Uses the Codex bundled Python environment with python-docx and Pillow.
Does not change any application code. Render and inspect the DOCX separately.
"""
from pathlib import Path
import hashlib
import json
import re
import subprocess
from datetime import datetime
from zoneinfo import ZoneInfo

from PIL import Image, ImageDraw, ImageFont
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "docs" / "beginner-guides"
ASSETS = OUT / "assets"
QA = ROOT / "artifacts" / "login-guide-qa"
MD = OUT / "Nexora login page.md"
DOCX = OUT / "Nexora login page.docx"
SNAPSHOT_DATE = datetime.now(ZoneInfo("Europe/London"))
AUTH = "apps/web/src/app/(auth)/login/"
IDENTITY = "backend/src/Modules/Nexora.Modules.Identity/"
SOURCES = [
    ("F01", AUTH + "page.tsx", "Page structure and destination", None),
    ("F02", AUTH + "login-form.tsx", "Form and browser behaviour", None),
    ("F03", AUTH + "login.module.css", "Login appearance", None),
    ("F04", AUTH + "galaxy-background.tsx", "Animated background", None),
    ("F05", "apps/web/next.config.ts", "API forwarding", None),
    ("F06", "apps/web/src/lib/auth/session.ts", "Session checking", None),
    ("F07", "apps/web/src/app/(platform)/layout.tsx", "Protected workspace layout", None),
    ("F08", "apps/web/src/components/brand/nexora-mark.tsx", "Shared logo", None),
    ("F09", "apps/web/src/components/ui/button.tsx", "Shared button", None),
    ("F10", AUTH + "help/page.tsx", "Login help page", None),
    ("B01", IDENTITY + "IdentityModule.cs", "Authentication setup and endpoints", None),
    ("B02", IDENTITY + "Security/LocalIdentityAdapter.cs", "Local credential verification", None),
    ("B03", IDENTITY + "Domain/NexoraUser.cs", "User record", None),
    ("B04", IDENTITY + "Security/NexoraClaimsPrincipalFactory.cs", "Tenant and permission claims", None),
    ("B05", IDENTITY + "Security/ActiveTenantAuthorization.cs", "Active tenant authorisation", None),
    ("B06", "backend/src/Nexora.Api/Program.cs", "API startup", None),
    ("B07", IDENTITY + "Infrastructure/Persistence/NexoraIdentityDbContext.cs", "Identity database mapping", None),
    ("B08", IDENTITY + "Infrastructure/Persistence/IdentitySeeder.cs", "Development identity seed", None),
    ("S01", "apps/web/src/app/layout.tsx", "Root layout and title", None),
    ("S02", "apps/web/src/app/globals.css", "Global imports and base styles", [(1, 42)]),
    ("S03", "apps/web/src/components/app-shell/workspace-theme.css", "Shared default colour variables", [(1, 8)]),
    ("S04", "apps/web/src/lib/cn.ts", "Class merge helper", None),
    ("S05", "backend/src/Nexora.Api/appsettings.Development.json", "Development provider configuration", None),
    ("S06", IDENTITY + "Security/IdentityProviderOptions.cs", "Provider option model", None),
    ("S07", IDENTITY + "Security/NexoraSecurity.cs", "Permission names and tenant context", None),
    ("S08", IDENTITY + "Domain/Tenant.cs", "Tenant record", None),
    ("S09", IDENTITY + "Domain/AuditEvent.cs", "Audit record", None),
    ("S10", IDENTITY + "Infrastructure/Persistence/TenantWriteBoundary.cs", "System access scope", [(1, 24)]),
    ("S11", "apps/web/src/app/(platform)/dashboard/page.tsx", "Protected dashboard route", None),
    ("S12", "apps/web/src/components/app-shell/app-shell.tsx", "Staff sign out handler", [(42, 56)]),
]

ASSETS.mkdir(parents=True, exist_ok=True)
QA.mkdir(parents=True, exist_ok=True)
source_data = {}
manifest = []
for fid, relative, description, ranges in SOURCES:
    file = ROOT / relative
    raw = file.read_bytes()
    source = raw.decode("utf-8-sig").splitlines()
    selected = [(i, line) for i, line in enumerate(source, 1)
                if ranges is None or any(a <= i <= b for a, b in ranges)]
    source_data[fid] = dict(path=relative, lines=source, selected=selected,
                            sha=hashlib.sha256(raw).hexdigest(), description=description)
    manifest.append(dict(id=fid, path=relative, sha256=source_data[fid]["sha"],
                         line_count=len(source), included_ranges=ranges or [[1, len(source)]]))


def font(size, bold=False, mono=False):
    name = "consola.ttf" if mono else ("arialbd.ttf" if bold else "arial.ttf")
    return ImageFont.truetype(str(Path("C:/Windows/Fonts") / name), size)


def wrap_text(draw, text, fnt, width):
    result = []
    for para in text.split("\n"):
        indent = re.match(r"^ *", para)[0]
        line = ""
        for word in para.split(" "):
            candidate = f"{line} {word}".strip()
            if draw.textlength(candidate, font=fnt) > width and line:
                result.append(line)
                line = word
            else:
                line = candidate
        result.append(indent + line)
    return result


def node(draw, box, title, body, colour="#f3f5f8", title_size=30, body_size=25):
    x1, y1, x2, y2 = box
    draw.rounded_rectangle(box, radius=15, fill=colour, outline="#7b8794", width=2)
    y = y1 + 20
    for line in wrap_text(draw, title, font(title_size, True), x2 - x1 - 38):
        draw.text((x1 + 19, y), line, fill="#000000", font=font(title_size, True))
        y += title_size + 8
    y += 7
    for line in wrap_text(draw, body, font(body_size), x2 - x1 - 38):
        draw.text((x1 + 19, y), line, fill="#26313e", font=font(body_size))
        y += body_size + 8
    if y > y2 - 8:
        raise ValueError(f"Diagram box too short for {title}")


def arrow(draw, start, end):
    import math
    draw.line([start, end], fill="#536879", width=4)
    theta = math.atan2(end[1] - start[1], end[0] - start[0])
    pts = [end, (end[0] - 15 * math.cos(theta - .5), end[1] - 15 * math.sin(theta - .5)),
           (end[0] - 15 * math.cos(theta + .5), end[1] - 15 * math.sin(theta + .5))]
    draw.polygon(pts, fill="#536879")


def make_diagrams():
    im = Image.new("RGB", (1580, 1100), "white")
    d = ImageDraw.Draw(im)
    d.text((36, 22), "Follow the file IDs to find the source", fill="black", font=font(35, True))
    node(d, (38, 90, 510, 330), "The login screen", "F01 page.tsx\nF02 login-form.tsx\nF03 login.module.css\nF04 galaxy-background.tsx", "#e9f8f4")
    node(d, (570, 90, 1000, 330), "The shared pieces", "F08 logo   F09 button\nF10 help page\nS01 root layout\nS02 and S03 theme", "#f3f0fa")
    node(d, (1060, 90, 1535, 330), "The local addresses", "CRM web on 3017\nShared API on 5097\nSQL Express NexoraDev\nLaunch choice CRM")
    node(d, (38, 410, 510, 585), "F05 next.config.ts", "Forwards /api/... from the web server to the C# API", "#e9f8f4")
    node(d, (570, 410, 1000, 585), "B01 IdentityModule.cs", "Defines endpoints, CSRF, cookies and sign-in rules", "#eef4fa")
    node(d, (1060, 410, 1535, 585), "B02 LocalIdentityAdapter.cs", "Finds the account, verifies its password and records the outcome", "#eef4fa", title_size=28)
    node(d, (1060, 650, 1535, 860), "Identity database", "B03 user record\nB07 table mapping\nB08 development seed", "#f3f0fa")
    node(d, (38, 685, 510, 860), "F06 session.ts", "Reads the cookie and checks the session with the API", "#e9f8f4")
    node(d, (570, 685, 1000, 860), "F07 platform layout", "Requires a session before it shows the staff workspace", "#e9f8f4")
    arrow(d, (270, 330), (270, 410))
    arrow(d, (510, 499), (570, 499))
    arrow(d, (1000, 499), (1060, 499))
    arrow(d, (1295, 585), (1295, 650))
    arrow(d, (510, 780), (570, 780))
    d.line([(270, 685), (270, 636), (785, 636)], fill="#536879", width=4)
    arrow(d, (785, 636), (785, 585))
    d.text((354, 602), "Session request goes direct to API", fill="#26313e", font=font(24))
    d.text((38, 936), "B04 adds permissions to the identity. B05 checks the active tenant.", fill="black", font=font(29))
    d.text((38, 986), "Open a file using Ctrl+P. Use section 4 for its full path.", fill="black", font=font(29))
    im.save(ASSETS / "login-code-map.png")

    im = Image.new("RGB", (1580, 940), "white")
    d = ImageDraw.Draw(im)
    d.text((35, 24), "VS Code learning map", fill="black", font=font(38, True))
    d.rounded_rectangle((35, 90, 1545, 865), radius=12, fill="#f7f8fa", outline="#7b8794", width=2)
    d.rectangle((35, 90, 1545, 160), fill="#e3e8ef")
    d.text((61, 108), "File > Open Folder     D:\\Projects new\\Nexora", fill="black", font=font(29, True))
    node(d, (55, 184, 450, 632), "1 Explorer", "apps\n  web\n    src\n      app\n        (auth)\n          login\n            page.tsx\n            login-form.tsx", title_size=30, body_size=27)
    node(d, (474, 184, 1520, 632), "2 Editor", "Ctrl+P opens a file by path\nCtrl+F finds a phrase in this file\nCtrl+G goes to a line number\nCtrl+S saves your change\n\nExample find Welcome back in page.tsx\nChange the words inside the h1 tag", "#e9f8f4", body_size=29)
    node(d, (55, 663, 450, 835), "3 Run and Debug", "Choose CRM then press F5\nView > Run opens this area", "#eef4fa", body_size=26)
    node(d, (474, 663, 1520, 835), "4 Terminal and Problems", "Terminal > New Terminal opens commands\nView > Problems shows reported code issues", body_size=29)
    im.save(ASSETS / "vscode-learning-map.png")

    im = Image.new("RGB", (1580, 1500), "white")
    d = ImageDraw.Draw(im)
    d.text((38, 20), "What happens when you sign in", fill="black", font=font(38, True))
    rows = [
        ("1 Open login", "F01 renders the page and F02 renders the form"),
        ("2 Ask which provider is available", "GET /api/v1/auth/providers through F05 to B01"),
        ("3 Get the security token", "GET /api/v1/auth/csrf returns token plus an antiforgery cookie"),
        ("4 Send credentials and token", "POST /api/v1/auth/login sends email, password and rememberMe"),
        ("5 Check the account", "B01 checks CSRF then B02 checks account, tenant and password"),
        ("6 Return success and a session cookie", "Identity signs in, B04 supplies claims and B02 records the outcome"),
        ("7 Navigate and verify the session", "F02 opens the destination, then F07 and F06 check /auth/session"),
    ]
    y = 94
    for i, (title, body) in enumerate(rows):
        node(d, (100, y, 1480, y + 145), title, body, "#e9f8f4" if i % 2 == 0 else "#eef4fa", 33, 28)
        if i < len(rows) - 1:
            arrow(d, (790, y + 145), (790, y + 188))
        y += 194
    d.text((100, 1460), "If a login step fails, F02 displays an error. Section 12 explains what to check.", fill="black", font=font(27))
    im.save(ASSETS / "login-request-journey.png")


def source_link(fid):
    relative = source_data[fid]["path"]
    return f"[{relative}](<{(ROOT / relative).as_posix()}>)"


def replace_refs(text):
    # Re-run safely after placeholders have already been expanded.
    text = re.sub(r"\{\{file:([A-Z]\d+)\}\}", lambda m: source_link(m[1]), text)
    def locate(m):
        fid, anchor = m[1], m[2]
        numbers = [i for i, line in enumerate(source_data[fid]["lines"], 1) if anchor in line]
        if not numbers:
            raise ValueError(f"Cannot find source anchor {fid} {anchor}")
        return f"line {numbers[0]}"
    return re.sub(r"\{\{line:([A-Z]\d+):(.*?)\}\}", locate, text)


def build_markdown():
    original = MD.read_text(encoding="utf-8")
    head = original.split("<!-- BEGIN GENERATED LOGIN SOURCE -->")[0]
    head = replace_refs(head)
    head = re.sub(r"Source snapshot date .*", "Source snapshot date " + SNAPSHOT_DATE.strftime("%d %B %Y").lstrip("0"), head, count=1)
    appendix = ["<!-- BEGIN GENERATED LOGIN SOURCE -->", ""]
    for fid, relative, description, ranges in SOURCES:
        entry = source_data[fid]
        language = {".tsx": "tsx", ".ts": "typescript", ".css": "css", ".cs": "csharp", ".json": "json"}[Path(relative).suffix]
        coverage = "Complete file" if ranges is None else "Excerpt with original line numbers"
        appendix.extend([f"### {fid} {description}", "", source_link(fid), "",
                         f"{coverage}. {len(entry['selected'])} source lines shown.", "",
                         f"SHA256 `{entry['sha']}`", "", f"```{language}"])
        appendix.extend(f"{number:04d}  {line}" for number, line in entry["selected"])
        appendix.extend(["```", ""])
    appendix.extend(["<!-- END GENERATED LOGIN SOURCE -->", ""])
    result = head + "\n".join(appendix)
    if re.search(r"\{\{(?:file|line):", head):
        raise ValueError("Unresolved template reference")
    MD.write_text(result, encoding="utf-8")
    return result


def set_font(style, name, size, bold=False):
    style.font.name = name
    style.font.size = Pt(size)
    style.font.bold = bold
    style.font.color.rgb = RGBColor(0, 0, 0)
    rpr = style.element.get_or_add_rPr()
    rf = rpr.find(qn("w:rFonts"))
    if rf is None:
        rf = OxmlElement("w:rFonts")
        rpr.insert(0, rf)
    for attr in ["ascii", "hAnsi", "eastAsia", "cs"]:
        rf.set(qn(f"w:{attr}"), name)


def hyperlink(p, label, target):
    from docx.opc.constants import RELATIONSHIP_TYPE as RT
    rel = p.part.relate_to(target, RT.HYPERLINK, is_external=True)
    elem = OxmlElement("w:hyperlink")
    elem.set(qn("r:id"), rel)
    run = OxmlElement("w:r")
    props = OxmlElement("w:rPr")
    col = OxmlElement("w:color")
    col.set(qn("w:val"), "263D55")
    props.append(col)
    run.append(props)
    text = OxmlElement("w:t")
    text.text = label
    run.append(text)
    elem.append(run)
    p._p.append(elem)


INLINE = re.compile(r"(\[[^\]]+\]\(<[^>]+>\)|\[[^\]]+\]\([^)]+\)|\*\*.*?\*\*|`[^`]+`)")


def inline(p, text):
    for part in INLINE.split(text):
        if not part:
            continue
        if part.startswith("["):
            m = re.match(r"\[([^\]]+)\]\((?:<([^>]+)>|([^)]*))\)", part)
            if m:
                hyperlink(p, m[1], m[2] or m[3])
                continue
        r = p.add_run(part[2:-2] if part.startswith("**") else part[1:-1] if part.startswith("`") else part)
        if part.startswith("**"):
            r.bold = True
        if part.startswith("`"):
            r.font.name = "Consolas"
            r.font.size = Pt(10)


def bookmark(p, name, index):
    start = OxmlElement("w:bookmarkStart")
    start.set(qn("w:id"), str(index))
    start.set(qn("w:name"), name)
    end = OxmlElement("w:bookmarkEnd")
    end.set(qn("w:id"), str(index))
    p._p.insert(0, start)
    p._p.append(end)


def internal_link(p, label, target):
    el = OxmlElement("w:hyperlink")
    el.set(qn("w:anchor"), target)
    run = OxmlElement("w:r")
    text = OxmlElement("w:t")
    text.text = label
    run.append(text)
    el.append(run)
    p._p.append(el)


def table(doc, rows):
    headers = rows[0]
    t = doc.add_table(rows=1, cols=len(headers))
    t.autofit = False
    usable = 7.0
    proportions = [0.36, 0.64] if len(headers) == 2 else [0.38, 0.35, 0.27]
    for col, ratio in zip(t.columns, proportions):
        col.width = Inches(usable * ratio)
    all_rows = [headers] + rows[2:]
    for i, cells in enumerate(all_rows):
        if i:
            t.add_row()
        row = t.rows[i]
        for j, text in enumerate(cells):
            cell = row.cells[j]
            cell.width = Inches(usable * proportions[j])
            p = cell.paragraphs[0]
            p.style = "Table Body"
            inline(p, text.strip())
            if i == 0:
                for r in p.runs:
                    r.bold = True
                shade = OxmlElement("w:shd")
                shade.set(qn("w:fill"), "E8EEF3")
                cell._tc.get_or_add_tcPr().append(shade)
            margins = OxmlElement("w:tcMar")
            for edge, value in [("top", 75), ("bottom", 75), ("left", 95), ("right", 95)]:
                e = OxmlElement(f"w:{edge}")
                e.set(qn("w:w"), str(value))
                e.set(qn("w:type"), "dxa")
                margins.append(e)
            cell._tc.get_or_add_tcPr().append(margins)
        pr = row._tr.get_or_add_trPr()
        pr.append(OxmlElement("w:cantSplit"))
        if i == 0:
            pr.append(OxmlElement("w:tblHeader"))
    doc.add_paragraph().paragraph_format.space_after = Pt(2)


def build_docx(markdown):
    doc = Document()
    section = doc.sections[0]
    section.page_width = Inches(8.5)
    section.page_height = Inches(11)
    section.top_margin = Inches(.65)
    section.bottom_margin = Inches(.65)
    section.left_margin = Inches(.75)
    section.right_margin = Inches(.75)
    section.footer_distance = Inches(.25)
    set_font(doc.styles["Normal"], "Arial", 11)
    doc.styles["Normal"].paragraph_format.space_after = Pt(7)
    doc.styles["Normal"].paragraph_format.line_spacing = 1.12
    for name, size in [("Title", 28), ("Subtitle", 13), ("Heading 1", 18), ("Heading 2", 13), ("Heading 3", 11)]:
        set_font(doc.styles[name], "Arial", size, name.startswith("Heading"))
        doc.styles[name].paragraph_format.space_before = Pt(15 if name.startswith("Heading") else 0)
        doc.styles[name].paragraph_format.space_after = Pt(8)
        doc.styles[name].paragraph_format.keep_with_next = True
    for name, size, face in [("Code Listing", 9.5, "Consolas"), ("Table Body", 10.5, "Arial"), ("Small", 9.5, "Arial")]:
        doc.styles.add_style(name, 1)
        set_font(doc.styles[name], face, size)
    code = doc.styles["Code Listing"].paragraph_format
    code.space_after = Pt(0)
    code.space_before = Pt(0)
    code.line_spacing = 1.04
    code.left_indent = Pt(33)
    code.first_line_indent = Pt(-33)
    code.keep_together = True
    doc.styles["Table Body"].paragraph_format.space_after = Pt(1)
    doc.styles["Table Body"].paragraph_format.line_spacing = 1.08
    doc.styles["Small"].paragraph_format.space_after = Pt(4)
    footer = section.footer.paragraphs[0]
    footer.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    footer.style = "Small"
    footer.add_run("Nexora login page    ")
    field = OxmlElement("w:fldSimple")
    field.set(qn("w:instr"), "PAGE")
    footer._p.append(field)
    doc.core_properties.title = "Nexora login page"
    doc.core_properties.subject = "Beginner guide to the staff CRM login"
    doc.core_properties.author = "Nexora"
    doc.core_properties.keywords = "Nexora, login, beginner, VS Code, frontend, backend"

    lines = markdown.splitlines()
    i, in_code, appendix, in_contents = 0, False, False, False
    code_paragraphs = []
    bkm = 1
    while i < len(lines):
        line = lines[i]
        if line.startswith("<!--"):
            i += 1
            continue
        if line.startswith("```"):
            in_code = not in_code
            if not in_code:
                # Keep the last few source lines together so a closing brace
                # does not land alone on an otherwise empty page.
                for p in code_paragraphs[-4:-1]:
                    p.paragraph_format.keep_with_next = True
                if code_paragraphs:
                    code_paragraphs[-1].paragraph_format.keep_with_next = False
                code_paragraphs = []
                doc.add_paragraph().paragraph_format.space_after = Pt(4)
            i += 1
            continue
        if in_code:
            p = doc.add_paragraph(style="Code Listing")
            r = p.add_run(line or " ")
            r.font.name = "Consolas"
            if len(code_paragraphs) < 3:
                p.paragraph_format.keep_with_next = True
            code_paragraphs.append(p)
            # Code is copied exactly. Word performs visual wrapping only.
            i += 1
            continue
        if not line.strip():
            i += 1
            continue
        if line.startswith("| "):
            rows = []
            while i < len(lines) and lines[i].startswith("|"):
                rows.append(lines[i].strip().strip("|").split("|"))
                i += 1
            table(doc, rows)
            continue
        if line.startswith("# "):
            doc.add_paragraph(line[2:], "Title")
        elif line.startswith("## "):
            title = line[3:]
            if title.startswith("16 "):
                appendix = True
            p = doc.add_paragraph(title, "Heading 1")
            if title.startswith("2 ") or title.startswith("16 "):
                p.paragraph_format.page_break_before = True
            m = re.match(r"(\d+) ", title)
            if m:
                bookmark(p, f"section_{m[1]}", bkm)
                bkm += 1
            in_contents = False
        elif line.startswith("### "):
            p = doc.add_paragraph(line[4:], "Heading 2")
        elif line.startswith("!["):
            m = re.match(r"!\[([^]]+)\]\(([^)]+)\)", line)
            image = OUT / m[2]
            p = doc.add_paragraph()
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p.paragraph_format.keep_with_next = True
            pic = p.add_run().add_picture(str(image), width=Inches(6.95))
            pic._inline.docPr.set("descr", m[1])
        elif line == "Contents":
            p = doc.add_paragraph("Contents", "Heading 2")
            in_contents = True
        elif in_contents and re.match(r"\d+\. ", line):
            number = re.match(r"(\d+)\. ", line)[1]
            p = doc.add_paragraph()
            p.paragraph_format.space_after = Pt(3)
            internal_link(p, line, f"section_{number}")
        else:
            numbered = re.match(r"\d+\. ", line)
            bullet = line.startswith("- ")
            p = doc.add_paragraph(style="Small" if line.startswith("SHA256") else "Normal")
            if bullet or numbered:
                p.paragraph_format.left_indent = Inches(.18)
                p.paragraph_format.first_line_indent = Inches(-.18)
            inline(p, ("• " + line[2:]) if bullet else line.rstrip())
            if appendix and (line.startswith("[") or line.startswith("Complete file.") or line.startswith("Excerpt ") or line.startswith("SHA256")):
                p.paragraph_format.keep_with_next = True
            if line.endswith("  "):
                p.paragraph_format.keep_with_next = True
                p.paragraph_format.space_after = Pt(2)
        i += 1
    # The stock Word Title style can contain a blue paragraph border.
    # Remove inherited and direct borders rather than relying on font colour.
    for style in doc.styles:
        for border in style.element.findall('.//' + qn('w:pBdr')):
            border.getparent().remove(border)
    for p in doc.paragraphs:
        for border in p._p.findall('.//' + qn('w:pBdr')):
            border.getparent().remove(border)
    doc.save(DOCX)
    return doc


def validate(markdown, doc):
    for fid, relative, _, _ in SOURCES:
        expected = "\n".join(f"{n:04d}  {line}" for n, line in source_data[fid]["selected"])
        if expected not in markdown:
            raise ValueError(f"Source snapshot mismatch {fid}")
    for p in doc.paragraphs:
        if p.style.name in ("Title", "Heading 1", "Heading 2") and ("{{" in p.text or not p.text.strip()):
            raise ValueError("Invalid heading")
    # Detect unchanged originals without exposing any local password.
    local = json.loads((ROOT / ".vscode/launch.json").read_text(encoding="utf-8-sig"))
    api = next(c for c in local["configurations"] if c["name"] == "Nexora shared API")
    password = api["env"].get("Identity__SeedAdminPassword")
    if password and password in markdown:
        raise ValueError("Local seed password must not be copied into the guide")
    (QA / "source-manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    try:
        head = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    except (subprocess.SubprocessError, OSError):
        head = None
    evidence = dict(docx=str(DOCX), markdown=str(MD), date=SNAPSHOT_DATE.strftime("%Y-%m-%d"), git_head=head,
                    source_files=len(SOURCES), source_lines=sum(len(v["selected"]) for v in source_data.values()),
                    snapshot_match=True, local_seed_password_included=False,
                    live_login_submission_performed=False)
    (QA / "build-evidence.json").write_text(json.dumps(evidence, indent=2), encoding="utf-8")
    print(json.dumps(evidence, indent=2))


if __name__ == "__main__":
    make_diagrams()
    md = build_markdown()
    document = build_docx(md)
    validate(md, document)
