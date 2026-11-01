"""Audit editorial coverage, artwork rarity and the pinned Wind Waker contract.

Developer-only: Python 3, Pillow and NumPy. No Cemu or user state is accessed.
Run: python -I tools/audit-achievement-catalog.py
The JSON report is written only under ignored test-results/.
"""
import hashlib
import json
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / 'tools/artwork/wind-waker-hd'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def main():
    plan = json.loads((ART / 'compositions.json').read_text(encoding='utf8'))
    rules = {}
    for name, expected in plan['catalog_contract'].items():
        path = ROOT / name
        require(sha(path) == expected, 'Rule contract changed: ' + name)
        doc = ET.parse(path).getroot()
        require(doc.get('revision') == 'wwhd-alpha-1', 'Unexpected rule revision')
        for node in doc:
            id = node.get('id')
            require(id not in rules, 'Duplicate rule: ' + id)
            rules[id] = dict(node.attrib, planned=node.tag == 'planned')
    presentation = ET.parse(ROOT / 'achievement-presentation.xml').getroot()
    require(presentation.get('revision') == 'wwhd-alpha-1', 'Presentation revision changed')
    require(len(presentation) == len(rules) == 110, 'Incomplete current catalog')
    entries = {a.get('id'): a.attrib for a in presentation}
    require(set(entries) == set(rules), 'Editorial ID coverage mismatch')
    require(len(set(a['title'].casefold() for a in entries.values())) == 110, 'Duplicate titles')
    icons = ROOT / 'assets/games/wind-waker-hd/icons'
    require({p.stem for p in icons.glob('*.png')} == set(rules), 'Icon ID coverage mismatch')
    for id, expected in plan['preserved_icons'].items():
        require(sha(icons / (id + '.png')) == expected, 'Preserved owner image changed: ' + id)
    templates = {}
    for tier in ('bronze', 'silver', 'gold'):
        spec = plan['templates'][tier]
        path = ART / spec['file']
        require(sha(path) == spec['sha256'], 'Template changed: ' + tier)
        templates[tier] = np.asarray(Image.open(path).convert('RGBA').resize((256, 256)))
    # Compare only the opaque outside ring shared by all three templates. The
    # subject, sky, counters and foreground boat cannot influence this score.
    y, x = np.ogrid[:256, :256]
    mask = ((x - 127.5) ** 2 + (y - 127.5) ** 2 > 105 ** 2)
    mask &= np.logical_and.reduce([a[:, :, 3] > 250 for a in templates.values()])
    compositions = {a['id']: a for a in plan['icons']}
    report = []
    for id, rule in rules.items():
        entry = entries[id]
        require(0 < len(entry['title']) <= 32, 'Title too long or blank: ' + id)
        require(0 < len(entry['description']) <= 100, 'Description too long or blank: ' + id)
        require(entry['rarity'] in templates, 'Unexpected current rarity: ' + id)
        path = icons / (id + '.png')
        image = Image.open(path)
        require(image.mode == 'RGBA' and image.size == (256, 256), 'Invalid runtime image: ' + id)
        pixels = np.asarray(image)
        scores = {tier: float(np.abs(pixels[mask, :3].astype(float) - reference[mask, :3]).mean())
                  for tier, reference in templates.items()}
        actual = min(scores, key=scores.get)
        require(actual == entry['rarity'], 'Artwork frame contradicts editorial rarity: ' + id)
        if id in compositions:
            require(compositions[id]['rarity'] == actual, 'Composition rarity mismatch: ' + id)
            require(compositions[id]['title'] == entry['title'], 'Stale composition title: ' + id)
        report.append(dict(id=id, title=entry['title'], description=entry['description'],
                           rarity=actual, frame_scores=scores, image_sha256=sha(path),
                           status='planned' if rule['planned'] else 'experimental' if rule.get('experimental') == 'true' else 'standard',
                           field=rule.get('field'), threshold=rule.get('threshold'),
                           points=rule['points'], chapter=entry.get('chapter', rule.get('chapter')),
                           order=int(entry.get('order', rule.get('order', 10000)))))
    # A numerical series must not reverse its thresholds in gameplay order.
    for ids in [['explore-chart-10', 'explore-chart-25', 'explore-chart-49'],
                ['explore-salvage-first', 'explore-salvage-10', 'explore-salvage-46'],
                ['collect-treasure-charts-10', 'collect-treasure-charts-25', 'collect-treasure-charts-46'],
                ['collect-heart-piece-first', 'collect-heart-pieces-12', 'collect-heart-pieces-44']]:
        order = [int(entries[id]['order']) for id in ids]
        require(order == sorted(order), 'Reversed collection milestones: ' + str(ids))
    result = dict(date='2026-11-01', review='editorial/artwork consistency with 40 intentional experimental rule promotions; no live detector certification',
                  count=len(report), rarities=dict(Counter(r['rarity'] for r in report)),
                  statuses=dict(Counter(r['status'] for r in report)),
                  pinned_rule_files=plan['catalog_contract'], preserved_owner_icons=len(plan['preserved_icons']),
                  frame_sample_pixels=int(mask.sum()), achievements=sorted(report, key=lambda a: (a['order'], a['id'])))
    output = ROOT / 'test-results/catalog-audit.json'
    output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    print('PASS', len(report), 'IDs, texts, images and matching frame rarities;', result['rarities'], result['statuses'])
    print('PASS pinned rules, unchanged revision, preserved owner art and ascending collection milestones')
    print('Report:', output)


if __name__ == '__main__':
    main()
