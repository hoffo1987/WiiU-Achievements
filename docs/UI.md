# User interface — current Wind Waker theme

The Windows x64 WinForms interface is custom-drawn. Presentation never changes
sampling, achievement conditions, IDs, points, revision or persistence.

![Main window](screenshots/main.png)

## Composition

The project logo is centred in the 80 px header, inside a 208×58 box. Profile
and connection status occupy the left; navigation occupies the right. Long
profile names cannot move the logo. The game wordmark is centred in its own
248×132 banner area, with no extra crest beneath it. A smaller completion dial
sits opposite. Status, configuration and three equal live-value columns occupy
a separate 64 px cream row below the picture.

The Games catalog uses one round 108 px badge centred above the readable title.
Its chrome stays neutral; only the Wind Waker objectives, dialogs and popups
use that game's palette and motifs. Future games have separate `GameStyle`s.

| Catalog | Detail |
| --- | --- |
| ![Catalog](screenshots/games.png) | ![Detail](screenshots/detail.png) |

## Owner artwork and original fallbacks

The owner supplied the project logo/trophy, two game logos, 28 medal icons,
rarity frames, wallpaper video and UI kit. The kit contains 40 prepared PNGs:
parchment, simple panels/corners/rules and wind/water ornaments. Use selected
pieces in suitable places rather than forcing the complete atlas on screen.
Do not rip game files or add unrelated proprietary artwork.

- `assets/brand/`: shared project logo and multi-size EXE trophy icon.
- `assets/games/wind-waker-hd/`: game logos, medal art, frames, sky and video.
- `ui/parchment.png`: the owner's original 512×512 seamless texture, tiled on
  bars, cards, status, settings and detail panels with readable foreground text.
- Wind ornaments sit in the sky and gaps; waves decorate the footer and cards.
- Simple gold/blue frames surround filters, configuration and detail panels.
  `SimpleFrame` scales corners uniformly and tiles straight edge strips at the
  same scale. The interior has independently tiled parchment: no illustrated
  centre or logo is stretched.
- Rarity controls medal metal, ribbon, glow and synthesised chime. Platinum is
  reserved for a future completion objective; no current target has that rarity.

`tools/prepare-ui-art.py` crops the owner's ZIP/logos, removes faint stray margins,
resizes with premultiplied alpha and isolates overlapping wave silhouettes.
`tools/prepare-game-art.py` prepares medal icons/frames/sky. These optional Python
tools are not runtime dependencies. Build resources are embedded; the installed
app does not depend on Downloads/Temp paths. Original GDI+ glyphs and the drawn
sea remain fallbacks. Nunito uses [SIL OFL 1.1](../assets/fonts/OFL.txt); keep it.
The owner requested publication of the current assets/source/docs to GitHub;
that is not a general redistribution licence for third-party works.

## Video

`VideoBanner.cs` decodes the embedded silent H.264 wallpaper through Windows
Media Foundation on a background MTA thread, with double buffering. It starts
at the part of the day matching the local clock, fits the island without changing
its aspect ratio, and extends the outer colours into wide banners. Ambient
playback pauses while the window is inactive or the setting is disabled.
Windows N/Server without media features, decoding failures and non-Windows
runtimes use the drawn sea fallback. No game capture or process write is involved.

## Rendering and performance

Static cards use a bounded lazy bitmap cache: twice the visible cards, minimum
48, maximum 400. Live footers, hover and unlock celebrations remain dynamic.
Layers cache header/footer/tab track/banner overlays; ornaments resample once
per device size. The backdrop copies only the updated area. Banner updates
follow decoded frames; scroll invalidation stays in the grid. Cached copies
use 1:1 device pixels. See measured results and limits in [VALIDATION](VALIDATION.md).

The former erased stripe through the chapter filter was paint culling: `Render`
checked a band ending above the lower filter row. It now includes the complete
filter band to `gridRect.Y`. Pixel regressions compare narrow repaints against
whole frames at 100/125/150/200 % offscreen scale. These checks do not assert that
the Windows display scaling setting was changed.

## Notifications

![Popup timeline](screenshots/toast-timeline.png)

The top-centre popup is layered, click-through and does not activate the game
window. Its 5.6 s sequence reveals a grey medal, turns it to colour, adds rarity
rays/ribbon/shine, then fades away. The chime is an original synthesised bell
arpeggio with a longer sequence for rarer medals. A normal-window fallback is
used if layered drawing fails. **Prova notifica** cycles rarity previews; verify
visibility in the intended Cemu fullscreen mode independently.

## Controls, accessibility and settings

A/X/Y connect/demo/disconnect; P pauses/resumes; G opens Games; C/F change
category/chapter. Tab and arrows move focus; Enter/Space activate; B/Esc close
panels; wheel/PageUp/PageDown scroll. Drawn widgets expose accessible names,
roles, bounds, state and actions. Settings use real text boxes; passwords are
masked even when drawn during panel animation.

Settings live in `%LOCALAPPDATA%\WiiUAchievements\settings.xml` and include game,
profile, Cemu path, sound and ambient toggles. Paths/profile are read-only during
a connection. Cloud login has its own settings page; see [CLOUD](CLOUD.md).

| Settings | Locked detail |
| --- | --- |
| ![Settings](screenshots/settings.png) | ![Locked detail](screenshots/detail-locked.png) |

## Reproduce public images

Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\render-docs.ps1`.
It builds in Temp, uses an isolated synthetic profile and fixed example dates,
and regenerates every PNG in `docs/screenshots/`. No Cemu, real progress,
credentials or personal log is read. The preview harness is not part of production.
The running root EXE is preserved. No ZIP is created.

`tests/run-ui.ps1` is the regression suite. `--preview=<png>`,
`--preview-toast=<png>` and `--preview-medals=<png>` are additional developer aids.

![Medals](screenshots/medals.png)
