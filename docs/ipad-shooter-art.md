# iPad prototype art

Built-in GPT Image produced four PNGs. The two atlases and background used at runtime are real generated image assets. The version-one effects atlas was rejected because halos touched adjacent cells.

## Generation prompts

### ships

Generate a production game asset PNG sheet with a REAL transparent background alpha, exactly four isolated top-down spacecraft sprites in a strict 2x2 grid. Each centered in its quadrant, large clear empty gutters and generous margin around each, no overlaps or other fragments. Top left player interceptor faces UP, champagne ivory enamel, deep navy and turquoise emissive trim, rounded classic arcade silhouette. Top right small coral scout enemy faces DOWN. Bottom left two-swept-wing dark orange and cream agile enemy faces DOWN. Bottom right heavier deep crimson and bronze elite enemy faces DOWN. Cohesive 2D orthographic top-down premium retro arcade style with crisp shaded metal, clearly visible at 32px, consistent lighting from upper left. No perspective, text, labels, gridlines, border, exhaust trailing out, or faux transparency.

### effectsV1

Create a real alpha transparent PNG sprite atlas with exactly TWELVE isolated objects, in STRICT four columns by three rows uniformly spaced, generous gutters, each centered within its own cell with no overlap. Row 1 left to right: red needle straight energy projectile pointing UP, amber double-arrow piercing projectile pointing UP, yellow split-fin spread projectile pointing UP, cyan curved fin homing projectile pointing UP. Row 2: violet round explosive projectile; bright emerald heart icon; cobalt blue shield crest icon; thin pale cyan circular energy ring. Row 3: four progressive separate circular explosion sprites, small vivid amber flash, medium radially expanding hot fragments, larger orange halo, faded smoky cyan-orange halo. Cohesive orthographic retro sci-fi arcade, clear silhouette and crisp edges, no text, no numbers, no panels, no grid marks, no characters. Transparent PNG alpha, not a checkerboard background.

### effects

A production game sprite atlas of EXACTLY 12 neatly spaced small opaque shapes on REAL TRANSPARENT ALPHA, 4 columns by 3 rows, landscape ratio 4:3. Draw each object at maximum 42 percent of its CELL WIDTH AND HEIGHT, leaving more than half of every cell empty. No particle or glow extends past that compact size. Every object is centered in the middle of its cell; do not draw cell borders. Row 1: red upright needle shot, orange upright double-chevron shot, yellow three-finned upright arrow shot, cyan upright curved homing shot. Row 2: round violet plasma projectile, green heart, cobalt shield emblem, thin cyan ring. Row 3: four separated animation frames of an arcade explosion, tiny yellow flash, growing orange burst with short sparks, red-orange open growing ring, dim orange-cyan finishing wisps. Visual style crisp cel-painted arcade game UI, compact hard silhouettes, limited glow, no sticker outline, no labels, no content touching outer or inner edges, no checkerboards. Use a large transparent alpha background and keep an identical gap between all twelve compact assets.

### arena

Production backdrop for a portrait-oriented top-down arcade shooter arena, aspect roughly 4:5. Deep midnight ink and teal interstellar starfield, subtle nebula clouds and a thin luminous dust belt sweeping diagonally in upper third, restrained desaturated petrol blue and a hint of rose on far horizon, faint fine star points. Dark low contrast throughout the central and lower areas to make brightly colored tiny spacecraft and bullets clearly readable, never feature a giant planet or distracting foreground. Painterly textured yet polished arcade art, fully edge-to-edge, no frame, no text, no interface, NO ships.

## Source and selection

Raw PNG copies and SHA-256 checks are kept with the local QA report. The original GPT Image outputs remain under the tool's generated-images directory. The optimized sprites use the nearest complete cells from the approved ships and compact effects atlases. Alpha was kept inside the crop and all sprites received four transparent pixels on each side and a centered Unity pivot.

| Source | Runtime selection |
| --- | --- |
| ships | player, enemy-scout, enemy-wing, enemy-elite, row-major |
| compact effects | straight, piercing, spread, homing, blast, heal, shield, ring, burst-1 through burst-4, row-major |
| arena | arena, full generated frame |

Unity imports each selected file as a single UGUI-compatible Sprite, with bilinear filtering and mipmaps off. The background uses its full frame and is covered by the actual view layout. Game View evidence is stored separately from the raw art.
