# Elevation

The VSA look has two shapes. Tokens give the shadow values. The theme file applies them with `::part()`, because Web Awesome has no token for these places.

| Look | Shadow | Components |
|---|---|---|
| Raised: a surface over the canvas | `--vsa-shadow-raise` | Button, card |
| Carved: a channel cut into the surface | `--vsa-shadow-carve` | Input, select, textarea, switch track, checkbox, radio, slider track, progress bar |
| Knob: a part that moves in a channel | `--vsa-shadow-knob` | Switch thumb, slider thumb |
| Pressed: a raised part that is down | `--vsa-shadow-press` | Button on press, switch track when on |

```html preview
<wa-button>Raised</wa-button>
<wa-input placeholder="Carved" style="width: 200px"></wa-input>
<wa-switch checked aria-label="Knob"></wa-switch>
```

## Web Awesome shadows

Popups, dialogs and drawers use these tokens.

```tokens ^--wa-shadow-[sml]$
```

## VSA shadows

```tokens ^--vsa-shadow-
```
