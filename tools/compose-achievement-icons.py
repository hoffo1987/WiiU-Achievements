"""Compose the owner's missing Wind Waker medals without generative editing.

Python 3 + Pillow, developer-only. All paths are relative to this repository.
  python -I tools/compose-achievement-icons.py fetch
  python -I tools/compose-achievement-icons.py render
  python -I tools/compose-achievement-icons.py install

fetch retrieves only the reviewed public PNGs in the manifest. render creates
1254 px editable-quality exports plus review sheets. install writes only the
listed runtime icons at 256 px and verifies the preserved owner originals.
No game files, model archives, account or API key are used.
"""
import argparse
import concurrent.futures
import hashlib
import json
import math
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
MANIFEST = ROOT / 'tools/artwork/wind-waker-hd/compositions.json'
CACHE = ROOT / 'test-results/art-sources'
OUTPUT = ROOT / 'output/achievement-icons'
ICONS = ROOT / 'assets/games/wind-waker-hd/icons'
SIZE = 1254


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def resize(image, size):
    return image.convert('RGBa').resize(size, Image.Resampling.LANCZOS).convert('RGBA')


def verify_preserved(plan):
    for name, expected in plan['preserved_icons'].items():
        if digest(ICONS / (name + '.png')) != expected:
            raise ValueError('An existing owner icon changed: ' + name)


def fetch(plan):
    CACHE.mkdir(parents=True, exist_ok=True)
    def one(item):
        key, spec = item
        path = CACHE / spec['file']
        if not path.exists():
            request = urllib.request.Request(spec['url'], headers={
                'User-Agent': 'Mozilla/5.0', 'Referer': spec['referer']})
            with urllib.request.urlopen(request, timeout=45) as response:
                data = response.read()
            # Decode before keeping a response as a PNG source.
            import io
            Image.open(io.BytesIO(data)).verify()
            path.write_bytes(data)
        if digest(path) != spec['sha256']:
            raise ValueError('Source changed; review before use: ' + key)
        return key
    with concurrent.futures.ThreadPoolExecutor(max_workers=5) as pool:
        for key in pool.map(one, plan['sources'].items()):
            print('source', key, flush=True)


def load_subject(path):
    image = Image.open(path).convert('RGBA')
    # The published Kreeb PNG uses a pure green chroma background. Remove only
    # that colour, preserving the muted green trousers and every other pixel.
    if path.name == 'kreeb.png':
        pixels = list(image.get_flattened_data())
        image.putdata([(r, g, b, 0 if g > 220 and r < 20 and b < 20 else a)
                       for r, g, b, a in pixels])
    bounds = image.getchannel('A').getbbox()
    if bounds is None:
        raise ValueError('Empty source: ' + str(path))
    return image.crop(bounds)


def make_background(frame, sky):
    # Flood only the empty central hole. Disconnected exterior transparency and
    # the frame's foreground boat stay untouched. A tiny dilation seals seams.
    barrier = frame.getchannel('A').point(lambda a: 255 if a >= 40 else 0).filter(ImageFilter.MaxFilter(3))
    ImageDraw.floodfill(barrier, (SIZE // 2, SIZE // 2), 128, thresh=0)
    if any(barrier.getpixel(p) == 128 for p in ((0,0),(SIZE-1,0),(0,SIZE-1),(SIZE-1,SIZE-1))):
        raise ValueError('The frame interior is not closed; refusing to fill the outside sky')
    hole = barrier.point(lambda a: 255 if a == 128 else 0).filter(ImageFilter.MaxFilter(5))
    background = sky.copy()
    background.putalpha(hole)
    silhouette = ImageChops.lighter(hole, frame.getchannel('A'))
    return background, silhouette


def place(canvas, subject, spec):
    subject = subject.copy()
    if spec.get('angle'):
        subject = subject.rotate(spec['angle'], Image.Resampling.BICUBIC, expand=True)
    x, y, w, h = spec['box']
    ratio = min(w / subject.width, h / subject.height)
    subject = resize(subject, (max(1, round(subject.width * ratio)), max(1, round(subject.height * ratio))))
    x, y = round(x + (w - subject.width) / 2), round(y + (h - subject.height) / 2)
    layer = Image.new('RGBA', canvas.size)
    layer.alpha_composite(subject, (x, y))
    alpha = layer.getchannel('A')
    # Broad lifted shadow plus a tighter contact shadow: both use the exact
    # subject silhouette, rather than a generic ellipse or a painted outline.
    for blur, opacity, offset in ((22, 0.58, (14, 19)), (5, 0.30, (5, 7))):
        mask = alpha.filter(ImageFilter.GaussianBlur(blur)).point(lambda a: round(a * opacity))
        shadow = Image.new('RGBA', canvas.size, (8, 30, 36, 0)); shadow.putalpha(mask)
        canvas.alpha_composite(shadow, offset)
    canvas.alpha_composite(layer)


def counter(canvas, text, rarity):
    if not text:
        return
    # Small useful denomination, original Nunito and code-drawn metal, allowing
    # collection thresholds to remain distinguishable at app-icon size.
    x, y, r = 931, 303, 83
    draw = ImageDraw.Draw(canvas)
    metal = {'bronze': '#c79050', 'silver': '#bdcfda', 'gold': '#f2cd59'}[rarity]
    draw.ellipse((x-r+5, y-r+9, x+r+5, y+r+9), fill=(6, 33, 42, 70))
    draw.ellipse((x-r, y-r, x+r, y+r), fill=metal, outline='#62472b', width=5)
    draw.ellipse((x-r+9, y-r+9, x+r-9, y+r-9), outline='#fff0c7', width=4)
    font = ImageFont.truetype(str(ROOT / 'assets/fonts/Nunito-ExtraBold.ttf'), 70 if len(text)<3 else 57)
    draw.text((x, y-3), text, font=font, fill='#183f4c', anchor='mm')


def render(plan, only=None):
    verify_preserved(plan)
    OUTPUT.mkdir(parents=True, exist_ok=True)
    sources = {}
    for key, spec in plan['sources'].items():
        path = CACHE / spec['file']
        if digest(path) != spec['sha256']:
            raise ValueError('Source mismatch: ' + key)
        sources[key] = load_subject(path)
    templates = {}
    for key, spec in plan['templates'].items():
        path = MANIFEST.parent / spec['file']
        if digest(path) != spec['sha256']:
            raise ValueError('Owner template changed: ' + key)
        templates[key] = Image.open(path).convert('RGBA')
    backgrounds = {tier: make_background(templates[tier], templates['sky-reference']) for tier in ('bronze','silver','gold')}
    manifest_path = OUTPUT / 'manifest.json'
    report_by_id = {item['id']: item for item in json.loads(manifest_path.read_text(encoding='utf8'))} if only and manifest_path.exists() else {}
    if only and only not in {spec['id'] for spec in plan['icons']}:
        raise ValueError('Unknown composition: ' + only)
    for spec in plan['icons']:
        if only and spec['id'] != only:
            continue
        frame = templates[spec['rarity']]
        background, silhouette = backgrounds[spec['rarity']]
        canvas = background.copy()
        for layer in spec['layers']:
            place(canvas, sources[layer['source']], layer)
        counter(canvas, spec.get('counter'), spec['rarity'])
        # The owner's frame and boat are the frontmost layer, occluding objects
        # naturally at their edges; silhouettes never spill into square corners.
        canvas.alpha_composite(frame)
        canvas.putalpha(ImageChops.multiply(canvas.getchannel('A'), silhouette))
        path = OUTPUT / (spec['title'] + '.png')
        canvas.save(path, optimize=True)
        report_by_id[spec['id']] = dict(id=spec['id'], title=spec['title'], rarity=spec['rarity'], file=path.name, sha256=digest(path))
        print('composed', spec['id'], spec['rarity'], flush=True)
    report = [report_by_id[spec['id']] for spec in plan['icons']]
    manifest_path.write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf8')
    sheets(plan)


def sheets(plan):
    font = ImageFont.truetype(str(ROOT / 'assets/fonts/Nunito-Bold.ttf'), 18)
    small = ImageFont.truetype(str(ROOT / 'assets/fonts/Nunito-Regular.ttf'), 15)
    for page in range(math.ceil(len(plan['icons']) / 20)):
        items = plan['icons'][page*20:(page+1)*20]
        sheet = Image.new('RGB', (1200, math.ceil(len(items)/4)*310), '#e4e8e6')
        draw = ImageDraw.Draw(sheet)
        for i, spec in enumerate(items):
            x, y = (i % 4)*300, (i // 4)*310
            image = resize(Image.open(OUTPUT/(spec['title']+'.png')).convert('RGBA'), (248,248))
            sheet.paste(image, (x+26,y+2), image)
            title = spec['title']
            if draw.textlength(title,font=font)>288:
                words=title.split(); half=len(words)//2;lines=[' '.join(words[:half]),' '.join(words[half:])]
            else:lines=[title]
            for n,line in enumerate(lines):draw.text((x+150,y+253+n*21),line,font=font,fill='#143f4b',anchor='mt')
            draw.text((x+150,y+294),spec['rarity'].upper(),font=small,fill='#576974',anchor='mt')
        sheet.save(OUTPUT/('preview-'+str(page+1)+'.png'))


def install(plan):
    verify_preserved(plan)
    # Rarity comes from current editorial metadata, never inferred from points.
    rarities={a.get('id'):a.get('rarity') for a in ET.parse(ROOT/'achievement-presentation.xml').getroot()}
    exports={e['id']:e for e in json.loads((OUTPUT/'manifest.json').read_text(encoding='utf8'))}
    for spec in plan['icons']:
        if spec['rarity'] != rarities[spec['id']]:
            raise ValueError('Rarity changed; review composition: '+spec['id'])
        path=OUTPUT/exports[spec['id']]['file']
        if digest(path) != exports[spec['id']]['sha256']:
            raise ValueError('Unreviewed export changed: '+str(path))
    for spec in plan['icons']:
        path=OUTPUT/exports[spec['id']]['file']
        resize(Image.open(path).convert('RGBA'), (256,256)).save(ICONS/(spec['id']+'.png'),optimize=True)
    verify_preserved(plan)
    print('Installed',len(plan['icons']),'icons; preserved',len(plan['preserved_icons']))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action',choices=('fetch','render','install'))
    parser.add_argument('--only', help='Render one achievement ID, retaining the other reviewed exports')
    args=parser.parse_args()
    plan=json.loads(MANIFEST.read_text(encoding='utf8'))
    if args.only and args.action != 'render':
        parser.error('--only is supported for render only')
    if args.action == 'render':
        render(plan, args.only)
    else:
        {'fetch':fetch,'install':install}[args.action](plan)
