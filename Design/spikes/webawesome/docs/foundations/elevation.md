# Elevation

The VSA look has two shapes. Tokens give the shadow values. The theme file applies them to Web Awesome components with `::part()`, because Web Awesome has no token for these places. A VSA component, for example the dial, reads them directly.

| Look | Shadow | Components |
|---|---|---|
| Raised: a surface over the canvas | `--vsa-shadow-raise` | Button, card, details |
| Carved: a channel cut into the surface | `--vsa-shadow-carve` | Input, select, textarea, switch track, checkbox, radio, slider track, progress bar |
| Deep carve: a wide round channel | `--vsa-shadow-dial` | Dial channel |
| Hole: a raised center in a channel | `--vsa-shadow-hole` | Dial center |
| Knob: a part that moves in a channel | `--vsa-shadow-knob` | Switch thumb, slider thumb |
| Pressed: a raised part that is down | `--vsa-shadow-press` | Button on press, switch track when on |

```html preview
<wa-button>Raised</wa-button>
<wa-input placeholder="Carved" style="width: 200px"></wa-input>
<wa-switch checked aria-label="Knob"></wa-switch>
<vsa-dial value="72" label="Deep carve"
  style="--size: 72px; --track-width: 8px"></vsa-dial>
```

## Web Awesome shadows

Popups, dialogs and drawers use these tokens.

```tokens ^--wa-shadow-[sml]$
```

## VSA shadows

```tokens ^--vsa-shadow-
```
