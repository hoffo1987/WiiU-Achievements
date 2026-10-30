"""Prepare the owner's UI ZIP and three logos (Python 3 + Pillow).

Usage: python tools/prepare-ui-art.py <ui.zip> <project.png> <badge.png> <title.png>
Only artwork is read from the ZIP. Preview/sprite sheets are source material,
never displayed as an atlas in the application. Outputs are embedded by build.ps1.
"""
import io
import os
import sys
import zipfile
from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
UI = os.path.join(ROOT, 'assets', 'games', 'wind-waker-hd', 'ui')
BRAND = os.path.join(ROOT, 'assets', 'brand')


def trim(im):
    im = im.convert('RGBA')
    # AI-supplied PNGs contain stray nearly invisible pixels far from the art.
    box = im.getchannel('A').point(lambda a: 255 if a > 32 else 0).getbbox()
    return im.crop(box) if box else im


def save(im, directory, name, maximum=768):
    im = trim(im)
    im = im.convert('RGBa')  # premultiplied resize avoids dark transparent fringes
    im.thumbnail((maximum, maximum), Image.Resampling.LANCZOS)
    im = im.convert('RGBA')
    os.makedirs(directory, exist_ok=True)
    im.save(os.path.join(directory, name + '.png'), optimize=True)
    return im


def main(archive, project, badge, title):
    with zipfile.ZipFile(archive) as z:
        entries = {os.path.basename(n): n for n in z.namelist() if n.lower().endswith('.png')}

        def read(name):
            return Image.open(io.BytesIO(z.read(entries[name]))).convert('RGBA')

        singles = {
            'angolo_alto_sinistra': 'corner-top-left', 'angolo_alto_destra': 'corner-top-right',
            'angolo_basso_sinistra': 'corner-bottom-left', 'angolo_basso_destra': 'corner-bottom-right',
            'fregio_triforza_600x120': 'crest', 'texture_pergamena_tela_512x512': 'parchment',
            'ornamento_nautico_dorato_con_onde_marine': 'nautical-bottom-left',
            'emblema_marino_dorato_con_triforce': 'sea-crest',
            'decoro_ondoso_oceanico_in_stile_cartoon': 'ocean-wave',
            'ornamento_di_vento_e_onde_celesti': 'wind-ribbon',
            'volute_di_vento_e_onde_celesti': 'wind-scroll',
            'ornamento_marino_dorato_d_angolo': 'nautical-top-right',
            'ornamento_nautico_dorato_tra_le_onde': 'nautical-bottom-right',
            'ornamento_d_onda_marina_dorata': 'gold-wave',
            'ornamento_nautico_d_angolo_in_onde_colorate': 'nautical-top-left',
        }
        for source, target in singles.items():
            save(read(source + '.png'), UI, target)

        sheet = read('cornicette_semplici_sheet.png')
        crops = {
            'panel-gold': (31, 40, 424, 311), 'panel-sea': (28, 332, 425, 685),
            'label-gold': (450, 40, 1105, 147), 'label-sea': (450, 177, 1106, 310),
            'label-gold-wave': (464, 348, 1089, 467), 'label-sea-wave': (461, 490, 1090, 625),
            'simple-top-left': (1138, 39, 1298, 200), 'simple-top-right': (1332, 40, 1504, 202),
            'simple-bottom-left': (1138, 250, 1310, 403), 'simple-bottom-right': (1325, 250, 1509, 406),
            'rule-gold': (1122, 461, 1520, 531), 'rule-sea': (1122, 554, 1519, 622),
        }
        # Wave bounding boxes overlap on the atlas. Follow each connected silhouette
        # so a crop cannot include a cut-off curl from the neighbouring sprite.
        strip = sheet.crop((0, 700, sheet.width, 850))
        for i, seed in enumerate([(100, 100), (345, 90), (580, 95), (850, 90), (1080, 90), (1380, 90)]):
            mask = strip.getchannel('A').point(lambda a: 255 if a > 32 else 0)
            if mask.getpixel(seed) != 255:
                raise ValueError('Missing wave silhouette: ' + str(i))
            ImageDraw.floodfill(mask, seed, 128)
            selected = mask.point(lambda a: 255 if a == 128 else 0).filter(ImageFilter.MaxFilter(3))
            sprite = strip.copy()
            sprite.putalpha(ImageChops.darker(strip.getchannel('A'), selected))
            save(sprite, UI, 'wave-' + str(i))
        winds = [(30, 878, 216, 987), (237, 865, 482, 977),
                 (501, 872, 766, 996), (816, 874, 1142, 991), (1167, 857, 1506, 999)]
        crops.update(('wind-' + str(i), box) for i, box in enumerate(winds))
        for name, box in crops.items():
            save(sheet.crop(box), UI, name)
        original = read('asset_sheet_originale.png')
        save(original.crop((35, 35, 721, 706)), UI, 'parchment-rich')
        save(original.crop((7, 763, 1531, 987)), UI, 'crest-wide')

    logo = Image.open(project).convert('RGBA')
    save(logo, BRAND, 'logo', 1024)
    # The trophy tile supplied as part of the owner's logo, also used by Explorer.
    icon = save(logo.crop((1488, 135, 1882, 463)), BRAND, 'app-icon', 256)
    tile = Image.new('RGBA', (256, 256))
    tile.alpha_composite(icon, ((256 - icon.width) // 2, (256 - icon.height) // 2))
    tile.save(os.path.join(BRAND, 'app.ico'), sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    game = os.path.dirname(UI)
    save(Image.open(badge), game, 'badge', 512)
    save(Image.open(title), game, 'logo', 768)


if __name__ == '__main__':
    main(*sys.argv[1:])
