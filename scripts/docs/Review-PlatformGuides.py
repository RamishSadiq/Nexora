"""Render all exported guide pages for visual inspection and find overflow.

PDFs and PNGs stay in ignored artifacts. This is documentation QA, not an
application regression. Review the generated images before delivery.
"""
from pathlib import Path
import json
import sys
import pypdfium2 as pdfium
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding='utf-8')
ROOT=Path(__file__).resolve().parents[2]
QA=ROOT/'artifacts/platform-guide-qa'
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
all_metadata=[]
for file in sorted((QA/'pdf').glob('*.pdf')):
    pdf=pdfium.PdfDocument(str(file));dest=QA/'render'/file.stem;dest.mkdir(parents=True,exist_ok=True)
    metadata=[];batch=[];overflow=[];sheet=0
    for n in range(len(pdf)):
        page=pdf[n];tp=page.get_textpage();text=tp.get_text_range()
        im=page.render(scale=110/72).to_pil().convert('RGB');im.save(dest/f'page-{n+1:03d}.png');batch.append(im)
        metadata.append(dict(page=n+1,chars=len(text),start=text[:160],end=text[-160:]))
        for i in range(tp.count_chars()):
            x1,y1,x2,y2=tp.get_charbox(i)
            if x1<25 or x2>page.get_width()-25 or y1<12 or y2>page.get_height()-20:
                overflow.append(dict(page=n+1,char=i,box=[x1,y1,x2,y2]))
        if len(batch)==4 or n==len(pdf)-1:
            sheet+=1;canvas=Image.new('RGB',(batch[0].width*2,(batch[0].height+35)*2),'#cbd1d8');draw=ImageDraw.Draw(canvas)
            start=n-len(batch)+2
            for k,img in enumerate(batch):
                x=(k%2)*img.width;y=(k//2)*(img.height+35);canvas.paste(img,(x,y+35));draw.text((x+12,y+4),f'Page {start+k}',font=FONT,fill='black')
            canvas.save(dest/f'review-{sheet:03d}.png');batch=[]
        tp.close();page.close()
    record=dict(document=file.stem,pages=len(pdf),review_sheets=sheet,small_pages=[p['page'] for p in metadata if p['chars']<100],overflow_chars=len(overflow))
    (dest/'pages.json').write_text(json.dumps(metadata,indent=2),encoding='utf-8');(dest/'overflow.json').write_text(json.dumps(overflow,indent=2),encoding='utf-8')
    all_metadata.append(record);pdf.close();print(json.dumps(record),flush=True)
(QA/'render-evidence.json').write_text(json.dumps(all_metadata,indent=2),encoding='utf-8')
