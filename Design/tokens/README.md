# Tokens

The tokens of the VSA Design System, as CSS custom properties. All names start with `--vsa-`.

| File | Level | Content |
|---|---|---|
| `fonts.css` | None | The font files from Google Fonts |
| `global.css` | Global | Raw values: colors, sizes, font families, weights, durations |
| `alias.css` | Alias | Meaning: color, gradient, shadow, radius, font, space, motion, focus, opacity, effect |
| `component.css` | Component | One value for each part of each atom, for example `--vsa-toggle-track-on`. See `docs/decisions/0003-component-tokens.md` |

## Alias groups

| Group | Example | Light and dark |
|---|---|---|
| `color` | `--vsa-color-surface`, `--vsa-color-state-done` | Different values |
| `gradient` | `--vsa-gradient-surface` | Different values. Some values are plain colors |
| `shadow` | `--vsa-shadow-raise`, `--vsa-shadow-carve` | Different values, except `glow` and `fill` |
| `radius` | `--vsa-radius-card`, `--vsa-radius-control` | The same |
| `font` | `--vsa-font-display`, `--vsa-font-size-body` | The same |
| `space` | `--vsa-space-xs` … `--vsa-space-xxl` | The same |

A component that needs a background reads the `gradient` group. Then the theme can use a gradient or a plain color, and the component does not change.

## Name rule for global colors

`--vsa-<family>-<n>`

- n = 1000 − 10 × L*, rounded to the nearest 5. L* is the CIELAB lightness. A higher number is a darker color.
- Family: `warm` if red > blue, `cool` if blue > red, `gray` if red = blue. A strong color uses its hue name: `orange`, `green`, `amber`, `red`, `blue`.

Examples: `#fbfbfa` has L* 98.6, so its name is `--vsa-warm-15`. `#26262a` has L* 15.3, so its name is `--vsa-cool-845`.

## Add a token

1. If the value is new, add a global token with the name rule.
2. Add the alias token in `alias.css`. If its value changes with the theme, write it in the light block and in the two dark blocks.
3. Open the gallery. Make sure that the token shows correctly in light and in dark.
